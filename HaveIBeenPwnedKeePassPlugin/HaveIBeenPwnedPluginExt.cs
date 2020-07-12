using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;

namespace HaveIBeenPwnedPlugin
{
    public sealed class HaveIBeenPwnedPluginExt : Plugin
    {
        private IPluginHost _pluginHost = null;
        private bool _checkPasswordOnEntryTouched = true;
        private bool _isIgnoreExpiredEntriesEnabled = true;
        private bool _isShowGoodNewsOnManualEntryCheckEnabled = false;
        private const string Option_CheckPasswordOnEntryTouched = "HaveIBeenPwnedPlugin_Option_CheckPasswordOnEntryTouched";
        private const string Option_IgnoreExpiredEntries = "HaveIBeenPwnedPlugin_Option_IgnoreExpiredEntries";
        private const string Option_ShowGoodNewsOnManualEntryCheck = "HaveIBeenPwnedPlugin_Option_ShowGoodNewsOnManualEntryCheck";
        private const string BreachedTag = "pwned";
        private const string IgnorePwnedTag = "pwned-ignore";
        private const string BreachCountCustomDataName = "pwned-count";
        private const string Message_NoEntrySelected = "No entry selected!";
        private readonly HIBP _hibp;

        public HaveIBeenPwnedPluginExt() => _hibp = new HIBP();

        public override string UpdateUrl => "https://raw.githubusercontent.com/kapsiR/HaveIBeenPwnedKeePassPlugin/main/KeePass.version";

        /// <summary>
        /// Initialization of the plugin
        /// </summary>
        /// <param name="pluginHost">Plugin host interface. Access the KeePass main window, the currently opened database, etc.</param>
        /// <returns><c>true</c> if plugin should be loaded within KeePass, otherwise <c>false</c></returns>
        public override bool Initialize(IPluginHost pluginHost)
        {
            if (pluginHost == null)
            {
                return false;
            }

            _pluginHost = pluginHost;

            try
            {
                // Supported protocols for HIBP are >= TLS 1.2
                // https://haveibeenpwned.com/API/v2#HTTPS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
                MessageService.ShowWarning($"Can't load {Assembly.GetAssembly(typeof(HaveIBeenPwnedPluginExt)).GetName()}",
                                           $"{nameof(SecurityProtocolType)} '{nameof(SecurityProtocolType.Tls12)}' is not supported!");

                return false;
            }

            InitializeCustomConfigSettings();
            InitializeEvents();

            return true;
        }

        private void InitializeEvents()
        {
            PwEntry.EntryTouched += PwEntry_TouchedAsync;
        }

        private void InitializeCustomConfigSettings()
        {
            _checkPasswordOnEntryTouched = _pluginHost.CustomConfig.GetBool(Option_CheckPasswordOnEntryTouched, true);
            _isIgnoreExpiredEntriesEnabled = _pluginHost.CustomConfig.GetBool(Option_IgnoreExpiredEntries, true);
            _isShowGoodNewsOnManualEntryCheckEnabled = _pluginHost.CustomConfig.GetBool(Option_ShowGoodNewsOnManualEntryCheck, false);
        }

        private async void PwEntry_TouchedAsync(object o, ObjectTouchedEventArgs e)
        {
            if (!_checkPasswordOnEntryTouched)
            {
                return;
            }

            if (e.Modified && e.Object is PwEntry pwEntry)
            {
                if (CheckEntryIgnoreConditions(pwEntry) != PwEntryIgnoreState.None)
                {
                    return;
                }

                await CheckHaveIBeenPwnedForEntry(pwEntry);
            }
        }

        /// <summary>
        /// Checks whether the entry should be ignored or not
        /// Any other <c>PwEntryIgnoreState</c> than <c>PwEntryIgnoreState.None</c> will be ignored
        /// </summary>
        /// <param name="pwEntry">The password entry</param>
        /// <returns>The <c>PwEntryIgnoreState</c> with the ignore reason</returns>
        private PwEntryIgnoreState CheckEntryIgnoreConditions(PwEntry pwEntry)
        {
            if (_isIgnoreExpiredEntriesEnabled && pwEntry.IsExpired())
            {
                return PwEntryIgnoreState.IsExpired;
            }

            if (pwEntry.HasTag(IgnorePwnedTag))
            {
                return PwEntryIgnoreState.IsIgnored;
            }

            return PwEntryIgnoreState.None;
        }

        private async Task CheckHaveIBeenPwnedForEntry(PwEntry pwEntry, Action executesWhenEntryIsGood = null)
        {
            if (pwEntry == null)
            {
                throw new ArgumentNullException(nameof(pwEntry));
            }

            try
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    var (isBreached, breachCount) = await _hibp.CheckRangeAsync(ComputeSha1HexPartsFromEntry(pwEntry, sha1));

                    bool entryModified = false;
                    if (isBreached)
                    {
                        entryModified = UpdateEntryWithBreachInformation(pwEntry, breachCount);

                        MessageService.ShowWarning($"Oh no. This password is breached and has been seen {breachCount} times before!",
                                                   $"Source: https://haveibeenpwned.com");
                    }
                    else
                    {
                        entryModified = UpdateEntryRemoveBreachInformation(pwEntry);
                        executesWhenEntryIsGood?.Invoke();
                    }

                    UpdateUI_EntryList(entryModified);
                }
            }
            catch (HttpRequestException)
            {
                SetHibpErrorState();
            }
        }

        private void SetHibpErrorState()
        {
            UnsubscribeEvents();

            MessageService.ShowWarning("There was a connectivity error while checking the HIBP Password Api.",
                                       "Automatic checks, e.g. when an entry gets modified, are disabled until you restart KeePass.",
                                       "You can still use all manual functions once connectivity is restored.");
        }

        /// <summary>
        /// Removes breach tag from entry
        /// </summary>
        /// <param name="pwEntry"></param>
        /// <param name="removedPwnedTagCount"></param>
        /// <returns><c>true</c>, if any breach tag has been removed, otherwise false</returns>
        private bool UpdateEntryRemoveBreachInformation(PwEntry pwEntry)
        {
            if (pwEntry.Tags.Contains(BreachedTag))
            {
                pwEntry.Tags.Remove(BreachedTag);
                pwEntry.CustomData.Remove(BreachCountCustomDataName);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds breach tag to entry
        /// </summary>
        /// <param name="pwEntry"></param>
        /// <param name="breachCount"></param>
        /// <returns><c>true</c>, if breach tag has been newly added, otherwise <c>false</c></returns>
        private bool UpdateEntryWithBreachInformation(PwEntry pwEntry, int breachCount)
        {
            if (!pwEntry.Tags.Contains(BreachedTag))
            {
                pwEntry.AddTag(BreachedTag);

                if (breachCount > 0)
                {
                    pwEntry.CustomData.Set(BreachCountCustomDataName, breachCount.ToString());
                }

                return true;
            }

            return false;
        }

        public override void Terminate()
        {
            SaveCurrentCustomConfigSettings();
            UnsubscribeEvents();

            _hibp.Dispose();
        }

        private void UnsubscribeEvents()
        {
            PwEntry.EntryTouched -= PwEntry_TouchedAsync;
        }

        private void SaveCurrentCustomConfigSettings()
        {
            _pluginHost.CustomConfig.SetBool(Option_CheckPasswordOnEntryTouched, _checkPasswordOnEntryTouched);
            _pluginHost.CustomConfig.SetBool(Option_IgnoreExpiredEntries, _isIgnoreExpiredEntriesEnabled);
            _pluginHost.CustomConfig.SetBool(Option_ShowGoodNewsOnManualEntryCheck, _isShowGoodNewsOnManualEntryCheckEnabled);
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Don't add menu items to tray
            if (t == PluginMenuType.Tray)
            {
                return null;
            }

            ToolStripMenuItem pluginRootMenuItem = new ToolStripMenuItem("Have I Been Pwned");

            ToolStripMenuItem checkCurrentPasswordMenuItem = new ToolStripMenuItem
            {
                Text = "Check current password"
            };
            checkCurrentPasswordMenuItem.Click += CheckCurrentPasswordMenuItem_ClickAsync;
            pluginRootMenuItem.DropDownItems.Add(checkCurrentPasswordMenuItem);

            ToolStripMenuItem checkAllPasswordsMenuItem = new ToolStripMenuItem
            {
                Text = "Check all passwords"
            };
            checkAllPasswordsMenuItem.Click += CheckAllPasswordsMenuItem_ClickAsync;
            pluginRootMenuItem.DropDownItems.Add(checkAllPasswordsMenuItem);

            pluginRootMenuItem.DropDownItems.Add(new ToolStripSeparator());

            if (t == PluginMenuType.Entry)
            {
                ToolStripMenuItem toggleCurrentPasswordIgnoreStateMenuItem = new ToolStripMenuItem
                {
                    Text = "Toggle ignore state"
                };
                toggleCurrentPasswordIgnoreStateMenuItem.Click += ToggleCurrentPasswordIgnoreStateMenuItem_Click;
                pluginRootMenuItem.DropDownItems.Add(toggleCurrentPasswordIgnoreStateMenuItem);

                pluginRootMenuItem.DropDownItems.Add(new ToolStripSeparator());
            }

            ToolStripMenuItem checkBoxMenuItem_Option_CheckPasswordOnEntryTouched = new ToolStripMenuItem
            {
                Text = "Check password when entry gets modified",
                Checked = _checkPasswordOnEntryTouched
            };
            checkBoxMenuItem_Option_CheckPasswordOnEntryTouched.Click += CheckBoxMenuItem_Option_CheckPasswordOnEntryTouched_Click;
            pluginRootMenuItem.DropDownItems.Add(checkBoxMenuItem_Option_CheckPasswordOnEntryTouched);

            ToolStripMenuItem checkBoxMenuItem_Option_SkipExpiredEntries = new ToolStripMenuItem
            {
                Text = "Skip expired entries",
                Checked = _isIgnoreExpiredEntriesEnabled
            };
            checkBoxMenuItem_Option_SkipExpiredEntries.Click += CheckBoxMenuItem_Option_SkipExpiredEntries_Click;
            pluginRootMenuItem.DropDownItems.Add(checkBoxMenuItem_Option_SkipExpiredEntries);

            ToolStripMenuItem checkBoxMenuItem_Option_ShowGoodNewsOnManualEntryCheck = new ToolStripMenuItem
            {
                Text = "Show good news when checking an entry manually too",
                Checked = _isShowGoodNewsOnManualEntryCheckEnabled
            };
            checkBoxMenuItem_Option_ShowGoodNewsOnManualEntryCheck.Click += CheckBoxMenuItem_Option_ShowGoodNewsOnManualEntryCheck_Click;
            pluginRootMenuItem.DropDownItems.Add(checkBoxMenuItem_Option_ShowGoodNewsOnManualEntryCheck);

            return pluginRootMenuItem;
        }

        private void CheckBoxMenuItem_Option_ShowGoodNewsOnManualEntryCheck_Click(object sender, EventArgs e)
        {
            _isShowGoodNewsOnManualEntryCheckEnabled = !_isShowGoodNewsOnManualEntryCheckEnabled;
            UIUtil.SetChecked(sender as ToolStripMenuItem, _isShowGoodNewsOnManualEntryCheckEnabled);
        }

        private void CheckBoxMenuItem_Option_SkipExpiredEntries_Click(object sender, EventArgs e)
        {
            _isIgnoreExpiredEntriesEnabled = !_isIgnoreExpiredEntriesEnabled;
            UIUtil.SetChecked(sender as ToolStripMenuItem, _isIgnoreExpiredEntriesEnabled);
        }

        private async void CheckCurrentPasswordMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (TryGetCurrentSelectedPwEntry(out PwEntry selectedEntry))
            {
                await CheckHaveIBeenPwnedForEntry(selectedEntry, () =>
                {
                    if (_isShowGoodNewsOnManualEntryCheckEnabled)
                    {
                        MessageService.ShowInfoEx("Good news", $"No pwnage found!",
                                                               $"Source: https://haveibeenpwned.com");
                    }
                });
            }
            else
            {
                MessageService.ShowInfo(Message_NoEntrySelected);
            }
        }

        private void ToggleCurrentPasswordIgnoreStateMenuItem_Click(object sender, EventArgs e)
        {
            if (TryGetCurrentSelectedPwEntry(out PwEntry selectedEntry))
            {
                if (selectedEntry.HasTag(IgnorePwnedTag))
                {
                    selectedEntry.RemoveTag(IgnorePwnedTag);
                }
                else
                {
                    selectedEntry.AddTag(IgnorePwnedTag);
                }

                UpdateUI_EntryList(true);
            }
            else
            {
                MessageService.ShowInfo(Message_NoEntrySelected);
            }
        }

        private bool TryGetCurrentSelectedPwEntry(out PwEntry currentSelectedPwEntry)
        {
            currentSelectedPwEntry = null;

            if (IsDatabaseOpen())
            {
                currentSelectedPwEntry = _pluginHost.MainWindow.GetSelectedEntry(true, false);

                if (currentSelectedPwEntry != null)
                {
                    return true;
                }
            }

            return false;
        }

        private async void CheckAllPasswordsMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (!IsDatabaseOpen())
            {
                return;
            }

            var entries = _pluginHost.Database.RootGroup.GetEntries(true) as IEnumerable<PwEntry>;

            int skippedExpiredEntriesCount = 0;
            int skippedIgnoredEntriesCount = 0;
            int checkedEntriesCount = 0;
            int pwnedEntriesCount = 0;
            int addedPwnedTagCount = 0;
            int removedPwnedTagCount = 0;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                int entriesCount = entries.Count();
                var statusBarLogger = _pluginHost.MainWindow.CreateStatusBarLogger();
                string statusText = "Checking all passwords against HIBP pwned passwords... {0}/" + entriesCount;
                statusBarLogger.StartLogging(string.Format(statusText, 0), false);
                double progress = 1;

                foreach (PwEntry entry in entries)
                {
                    checkedEntriesCount++;

                    var ignoreState = CheckEntryIgnoreConditions(entry);
                    switch (ignoreState)
                    {
                        case PwEntryIgnoreState.IsExpired:
                            skippedExpiredEntriesCount++;
                            continue;

                        case PwEntryIgnoreState.IsIgnored:
                            skippedIgnoredEntriesCount++;
                            continue;

                        case PwEntryIgnoreState.None:
                        default:
                            break;
                    }

                    (string sha1Prefix, string sha1Suffix) = ComputeSha1HexPartsFromEntry(entry, sha1);

                    try
                    {
                        var (isBreached, breachCount) = await _hibp.CheckRangeAsync(sha1Prefix, sha1Suffix);

                        if (isBreached)
                        {
                            if (UpdateEntryWithBreachInformation(entry, breachCount))
                            {
                                addedPwnedTagCount++;
                            }
                            pwnedEntriesCount++;
                        }
                        else
                        {
                            if (UpdateEntryRemoveBreachInformation(entry))
                            {
                                removedPwnedTagCount++;
                            }
                        }
                    }
                    catch (HttpRequestException)
                    {
                        SetHibpErrorState();
                        break;
                    }

                    statusBarLogger.SetText(string.Format(statusText, progress), KeePassLib.Interfaces.LogStatusType.Info);
                    statusBarLogger.SetProgress((uint)(progress++ / entriesCount * 100));
                }

                statusBarLogger.EndLogging();
            }

            if (addedPwnedTagCount > 0 || removedPwnedTagCount > 0)
            {
                UpdateUI_EntryList(true);
            }
            else
            {
                UpdateUI_EntryList(false);
            }

            MessageService.ShowInfo($"Checked entries: {checkedEntriesCount}",
                                    $"Pwned entries: {pwnedEntriesCount}",
                                    $"New pwned entries: {addedPwnedTagCount}",
                                    $"Entries not pwned anymore: {removedPwnedTagCount}",
                                    $"Skipped expired entries: {skippedExpiredEntriesCount}",
                                    $"Skipped ignored entries: {skippedIgnoredEntriesCount}");
        }

        private void UpdateUI_EntryList(bool setModified)
        {
            _pluginHost.MainWindow?.UpdateUI(false, null, false, null, true, null, setModified);
        }

        private (string sha1Prefix, string sha1Suffix) ComputeSha1HexPartsFromEntry(PwEntry entry, SHA1Managed sha1Managed)
        {
            string sha1Hex = BitConverter.ToString(sha1Managed.ComputeHash(entry.Strings.GetSafe("Password").ReadUtf8())).Replace("-", string.Empty);

            return (sha1Hex.Substring(0, 5), sha1Hex.Substring(5, sha1Hex.Length - 5));
        }

        private void CheckBoxMenuItem_Option_CheckPasswordOnEntryTouched_Click(object sender, EventArgs e)
        {
            _checkPasswordOnEntryTouched = !_checkPasswordOnEntryTouched;
            UIUtil.SetChecked(sender as ToolStripMenuItem, _checkPasswordOnEntryTouched);
        }

        private bool IsDatabaseOpen()
        {
            PwDatabase database = _pluginHost.Database;

            if (database != null && database.IsOpen)
            {
                return true;
            }

            return false;
        }
    }
}
