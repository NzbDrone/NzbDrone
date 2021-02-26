Name:           sonarr-bootstrap
Version:        %{BuildVersion}

Release:        1.%{?BuildBranch}
BuildArch:      noarch
Summary:        PVR for Usenet and BitTorrent users; self-updating package

License:        GPLv3+
URL:            https://sonarr.tv/
Source0:        https://download.sonarr.tv/v3/phantom-%{BuildBranch}/%{BuildVersion}/Sonarr.phantom-%{BuildBranch}.%{version}.linux.tar.gz
Source3:        sonarr.systemd
Source4:        sonarr.firewalld
Source5:        sonarr-secure.firewalld

BuildRequires:      systemd

Requires:           sqlite-libs >= 3.7
Requires:           mediainfo >= 0.7.52
Requires:           mono-complete

Requires(pre):      shadow-utils
Requires(postun):   shadow-utils

Requires(post):     systemd
Requires(preun):    systemd
Requires(postun):   systemd

Provides: /opt/sonarr/Sonarr.exe
Provides: sonarr
Conflicts: sonarr

# These prevent Sonarr's DLLs from auto-creating requires and provides
# Doing that because RH's mono require/provide detection isn't working
# right here 
# (thinks it requires a different version of a library than it provides type problems)
%global __provides_exclude_from ^/opt/sonarr/.*$
%global __requires_exclude_from ^/opt/sonarr/.*$

%description
Sonarr is a PVR for Usenet and BitTorrent users. It can monitor multiple RSS
feeds for new episodes of your favorite shows and will grab, sorts and renames
them. It can also be configured to automatically upgrade the quality of files
already downloaded when a better quality format becomes available.


%prep
%autosetup -n Sonarr

%build
# Empty build just to make rpmlint happier.
# This spec uses binaries built on windows by Sonarr team instead of
# attempting to build on Linux.

%install

# systemd service
install -m 0755 -d %{buildroot}%{_unitdir}
install -m 0644 %{SOURCE3} %{buildroot}%{_unitdir}/sonarr.service

# firewalld
install -m 0755 -d %{buildroot}%{_prefix}/lib/firewalld/services/
install -m 0644 %{SOURCE4} %{buildroot}%{_prefix}/lib/firewalld/services/sonarr.xml
install -m 0644 %{SOURCE5} %{buildroot}%{_prefix}/lib/firewalld/services/sonarr-secure.xml

# sonarr user in /var
install -m 0755 -d %{buildroot}%{_sharedstatedir}/sonarr

# sonarr software itself
install -m 0755 -d %{buildroot}/opt/sonarr


mv * %{buildroot}/opt/sonarr

find %{buildroot}/opt/sonarr -type f -exec chmod 644 '{}' \;
find %{buildroot}/opt/sonarr -type d -exec chmod 755 '{}' \;


%files
%defattr(0644,root,root,0755)
%dir %{_unitdir}
%{_unitdir}/sonarr.service

%dir %{_prefix}/lib/firewalld
%dir %{_prefix}/lib/firewalld/services
%{_prefix}/lib/firewalld/services/*.xml

%attr(0755,sonnar,sonnar) %dir /opt/sonarr
%verify(not md5 mode size mtime) %attr(-,sonarr,sonarr) /opt/sonarr/*

%attr(-,sonarr,sonarr)%{_sharedstatedir}/sonarr

%pre
getent group sonarr >/dev/null || groupadd -r sonarr
getent passwd sonarr >/dev/null || \
    useradd -r -g sonarr -d %{_sharedstatedir}/sonarr -s /sbin/nologin \
    -c "Sonarr PVR for Usenet and BitTorrent Users " sonarr
exit 0

%post
%systemd_post sonarr.service
%firewalld_reload
systemctl enable --now sonarr.service
firewall-cmd --add-service=sonarr --permanent

%preun
%systemd_preun sonarr.service
firewall-cmd --remove-service=sonarr --permanent

%postun
%systemd_postun_with_restart sonarr.service

## This is dangerous, rpmlint doesn't like it,
## and could break things if somebody uninstalls
## and reinstalls (instead of upgrade)
#if (($1==0)); then
#    if getent passwd sonarr &>/dev/null; then
#        userdel sonarr
#    fi
#    if getent group sonarr &>/dev/null; then
#        groupdel sonarr
#    fi
#fi

%changelog
* Fri Feb 26 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1132-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Cleanse Tracker Announce Keys from logs

* Sun Feb 21 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1131-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Refresh scene naming exceptions on series add to help first-use scenario
- Cleanse more /home/username scenarios

* Wed Feb 17 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1130-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: History details incorrect when preferred word score was 0
- Fixed: Searching specials with NNTMux-based usenet indexers
- Fixed: Debian package dependencies
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Use original file path when calculating preferred word score for existing file
- New: Include renamed file information for Webhook and Custom Scripts
- New: Include episode file with episode file deleted events
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Parsing of release names with trailing colon in the title
- Series editor column fixes

* Wed Feb 10 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1126-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Appeasing the lint gods
- Fixed: Unable to close indexer category select input on mobile
- Fixed: Error checking if files should be deleted after import won't leave import in limbo
- Use SVG for loading page icon
- Fixed: Error logged when notification fails to send after episode file is deleted
- New: Health check for import lists with missing root folders
- Fixed: Mark as Failed errors
- Fixed: Error logged when notification fails to send after episode file is deleted
- Fixed: Scene name not being set during import
- Fixed: Restoring backup from zip file on disk

* Mon Feb 08 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1117-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Errors loading queue after episodes in series are removed
- Fixed: Don't automatically import if absolutely numbered file if it doesn't match expected season
- Alternate titles prop validation
- Update column properties when restoring persisted state
- Fixed: Use file name when importing batch release when renaming is disabled
- New: Show preferred word score in history
- Generalized RateLimit logic to all indexers based on indexer id
- New: Added Hindi language
- Update parser tests to be generic
- Fixed: Table column order resetting after refresh
- New: Add logo to loading page
- Fixed: Jackett indexer search performance
- New: Added Arabic language

* Sun Feb 07 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1107-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Authentication on DSM 7
- Fixed: Settings fields being altered during save
- New: Persist search settings in add new series
- New: Show number of files as tooltip over size on disk
- Update feature request template
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: use-credentials for maniftest requests
- New: Add FileId to History data for import events

* Thu Feb 04 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1100-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Series Removed From TVDB wiki link
- Detect Dolby Vision as HDR and MediaInfo Update
- New: Add name field to release profiles

* Tue Feb 02 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1096-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Global scene mapping aliases disappeared from UI
- Fixed: Validation of new qbittorrent max-ratio action config
- Added searchEngine support in Newznab/Torznab caps
- Fixed: FLAC audio channels in media info

* Mon Feb 01 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1095-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: Disable season search if series is unmonitored
- Fixed: Handle more obfuscated names

* Sun Jan 31 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1093-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: On Episode File Delete For Upgrade notification option
- New: Unify series custom filter options

* Mon Jan 25 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1091-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Label for 'On Episode File Delete'
- Consistent types for on delete custom script events
- Fixed: Webhook events not sent for series deletions
- Separate event types for series and episode deletions
- Fixed: Queue refresh closing manual import from queue if items change

* Sun Jan 24 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1085-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: On Delete Notifications
- Fixed: Files with lower preferred word scores are imported
- Fixed: Series Type Filter
- Manual Import episode improvements
- Fixed: Improve multi-episode title squashing

* Thu Jan 21 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1083-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Update bug report template
- Lock closed issues after 90 days without activity
- New: Flood Download Client
- Typo for linux

* Mon Jan 18 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1077-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Error handling when cannot create folder in Recycling Bin

* Sun Jan 17 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1076-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: Treat Manual Bad in history as failed
- make HashedReleaseFixture entries generic
- Fixed: Handle more obfuscated names
- Fixed parsing (duplicate) releases for series with multiple season number mappings

* Sat Jan 16 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1073-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed accounting for zero terminator in long path limitation
- New: Require Encryption option for email
- Fixed: Managing display profiles on mobile
- Fixed: Sorting in Interactive search duplicates results
- Fixed duplicate id searches due to missing Equals on SceneSeasonMapping
- Show separate message for unknown episode/series
- Fixed: Regular Anime being caught in Chinese parser rules
- Fixed Agenda Time wrapping

* Fri Jan 15 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1069-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Updated BTN url to https
- Linting as usual
- Fixed: Unnecessary certificate validation errors on localhost/loopback
- New: Added Scene Info to Interactive Search results to show more about the applied scene/TheXEM mappings
- Fixed searching the wrong season.

* Thu Jan 14 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1066-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: Parsing of '[WEB]' as WebDL
- Update contributing.md
- Fix name of max NumberInput in QualityDefinition.js
- Readme updates
- New: Replace SmtpClient with Mailkit

* Wed Jan 13 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1062-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Parse standalone UHD as 2160p if no other resolution info is present

* Tue Jan 12 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1061-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Dailiezearch.

* Sun Jan 10 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1060-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Update wiki link hints for health checks

* Thu Jan 07 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1059-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: Allow quality size limits to be closer together
- Better task interval fetching
- Fixed: Only delete update folder if it exists

* Tue Jan 05 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1058-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed tests
- No longer need the special tvdb season number handling since it's integrated into the search.
- Fixed: Regression in searching anime by primary title
- New: Support in services for multiple scene naming/numbering exceptions
- Fixed: Backups interval being used as minutes instead of days

* Mon Jan 04 2021 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1052-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed tests
- Linting
- Fixed: Additional handling for obfuscated releases
- Fixed: Parsing of 4Kto1080p as 1080p
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Additional handling for obfuscated releases Closes #4198
- Fixed: Parsing of 4Kto1080p as 1080p Closes #4199
- Use createHandleActions for adding/removing commands so itemMap is synced properly
- New: Removing update folder from temp folder during housekeeping
- New: Renamed Quick Import to Move Automatically
- Fixed UpdatePackageProviderFixture tests
- Fixed: Don't convert series selection filter to lower case in state
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Restored robots.txt
- Fixed: Timespan over 1 month shown incorrectly
- Fixed: Missing leading 0 in minutes/seconds for media info duration
- Fixed: Backup interval is updated on change

* Thu Dec 31 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1042-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Update path before importing to ensure it hasn't changed
- Fixed: Parsing Polish language
- New: Rename Import to Library Import

* Sat Dec 26 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1039-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- eslint

* Fri Dec 25 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1035-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Small helper in UI to access Sonarr API more easily
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Series year wrong when airing January 1st.
- Fixed: OSX version detection

* Sat Dec 19 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1033-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Format Errors from AudioChannel formatter
- Fixed Migration 148 test
- Fixed: Handle 3 digit audio channels
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: Language parsing with space-delimited releases
- Fixed: Don't workaround DTS if audioChannels invalid
- Fixed: Migrate Mediainfo properties that changed names
- Fixed: Use audioChannels_Original if it exists in MI
- Fixed health check wiki link unit tests
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- New: Sorting Series List/Mass Editor by Language Profile and Tags

* Mon Dec 14 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1026-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- All Wiki links now use the consolidated Servarr wiki
- Fixed: '/series' URL Base breaking UI navigation
- New: Added Series Monitoring Toggle to Series Details

* Mon Dec 07 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1024-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Move config.yml for github
- Fixed: Using folder as scene name for season packs

* Wed Dec 02 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1023-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Update GitHub templates
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed: List Import no longer fails due to duplicates

* Mon Nov 23 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1021-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Removed unnecessary importlists warning.

* Sun Nov 22 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1020-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed binary execute permissions for osx and Radarr

* Sun Nov 22 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1019-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fixed disk permission tests

* Sat Nov 21 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1017-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Reverted temporary dev debug code change
- Fixed: Monitor 'None' won't monitor latest season
- New: Validate that naming formats don't contain illegal characters
- New: Displaying folder-based permissions in UI rather than file-based permissions and with selectable sane presets

* Wed Nov 18 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1011-1.develop
- Merge branch 'phantom-develop' of https://github.com/Sonarr/Sonarr into phantom-rpm-package
- Fix package_info file
- Update indexer category parameters for the other nyaa
- Dropping release back to 1, to prep for next Version update
- Minor typo in package_info

* Tue Nov 17 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1009-4.develop
- Bump release tag to get an update
- Fix useradd
- Update changelog

* Tue Nov 17 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.1009-1.develop
- Fork off a "bootstrap" version of the RPM that allows for self-update

* Fri Nov 13 2020 Eric Eisenhart <freiheit@gmail.com> - 3.0.4.994-10.develop
- RPM redone for Sonarr v3 beta
- auto-maintain the rpm changelog
- If tarball isn't there already, download from download.sonarr.tv
- Merge from orbisvicis/develop

* Fri Jan 02 2015 Yclept Nemo <"".join(chr(ord(c)-1) for c in "pscjtwjdjtAhnbjm/dpn")> - 2.0.0.2572-1.fc21
- Initial package