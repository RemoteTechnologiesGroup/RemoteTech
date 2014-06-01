#!/bin/bash

SRCDIR=src/RemoteTech2

if [ ! -f "$SRCDIR/Assembly-CSharp-firstpass.dll" ] \
   || [ ! -f "$SRCDIR/Assembly-CSharp.dll" ] \
   || [ ! -f "$SRCDIR/UnityEngine.dll" ];
then
   if [[ ! -f dlls.zip ]]; then
      echo "Need to get dependency .dll's"
      wget -O dlls.zip "https://www.dropbox.com/s/kyv25p3qn166nzp/dlls.zip?dl=1"
   fi
   
   if [ -z "$ZIPPASSWORD" ]; then
      if [ "$TRAVIS" = "true" ]; then
          echo "Password not set, on travis and DLL's missing, can't build";
          exit 1;
      else
          echo "Password required to decrypt the zip";
          unzip dlls.zip -d src/RemoteTech2/ # this will prompt for a password
      fi
   else
      unzip -P "$ZIPPASSWORD" dlls.zip -d src/RemoteTech2/
   fi
   
   rm -f dlls.zip
fi

cd src/RemoteTech2 && xbuild
