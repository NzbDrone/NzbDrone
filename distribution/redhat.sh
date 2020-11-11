#!/bin/sh

## TODO ##
# I (freiheit) did a few things to emulate my own lack of the real CI/CD
# environment that need to be done more properly in CI/CD:
# - Copy Sonarr.phantom-${BuildBranch}.${BuildVersion}.linux.tar.gz into ./redhat/
#
# Requirements:
# - mock and rpmbuild installed
# - user this is run as in mock group
#   (note: can use that to escalate to root if can change spec)
# - ran from "distribution" directory

BuildVersion=${dependent_build_number:-3.0.4.994}
BuildBranch=${dependent_build_branch:-develop}
BootstrapVersion=`echo "$BuildVersion" | cut -d. -f1,2,3`
BootstrapUpdater="BuiltIn"
SpecFile="redhat/sonarr-$BuildVersion-$BuildBranch.spec"

(
  echo "%define BuildVersion $BuildVersion"
  echo "%define BuildBranch $BuildBranch"
  echo
  cat redhat/sonarr.spec
) > $SpecFile


echo === Checking spec with rpmlint
# Ignore failure
rpmlint $SpecFile || true

echo === Fetch tarball if not present
if [ ! -e redhat/Sonarr.phantom-${BuildBranch}.${BuildVersion}.linux.tar.gz ]; then
  (
    cd redhat
    wget https://download.sonarr.tv/v3/phantom-${BuildBranch}/${BuildVersion}/Sonarr.phantom-${BuildBranch}.${BuildVersion}.linux.tar.gz 
  )
fi

echo === Cleaning out old SRPMs
rm -f *.src.rpm

echo === Building a .src.rpm package:
mock --buildsrpm -r epel-7-x86_64 --sources redhat  --spec $SpecFile --resultdir=./

echo === Uploading to copr to ask them to build for us
copr build --nowait sonarr-v3-test sonarr-${BuildVersion}-*.src.rpm || true

echo === Building for CentOS/RHEL/EPEL 8:
mock -r epel-8-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .el8"

echo === Building for CentOS/RHEL/EPEL 7:
mock -r epel-7-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .el7"

echo === Building for CentOS/RHEL/EPEL 6:
mock -r epel-7-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .el6"

echo === Building for Fedora 34:
mock -r fedora-34-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .fc34"

echo === Building for Fedora 33:
mock -r fedora-33-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .fc33"

echo === Building for Fedora 32:
mock -r fedora-32-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .fc32"

echo === Building for Fedora Rawhide:
mock -r fedora-rawhide-x86_64 sonarr-${BuildVersion}-*.src.rpm --resultdir=./ --define "dist .rawhide"

echo === Checking built RPMs with rpmlint
rpmlint *.rpm
