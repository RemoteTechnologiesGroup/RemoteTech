#!/bin/bash

SRCDIR=src/RemoteTech2

if [ ! -f "$SRCDIR/Assembly-CSharp-firstpass.dll" ] \
   || [ ! -f "$SRCDIR/Assembly-CSharp.dll" ] \
   || [ ! -f "$SRCDIR/UnityEngine.dll" ];
then
   echo "Need to get dependency .dll's"
   wget -O dlls.zip "https://www.dropbox.com/s/kyv25p3qn166nzp/dlls.zip?dl=1"
   unzip dlls.zip -d src/RemoteTech2/
   rm -f dlls.zip
fi

cd src/RemoteTech2 && xbuild
