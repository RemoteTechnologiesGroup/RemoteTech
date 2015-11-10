#!/bin/bash

SRCDIR=src/RemoteTech

if [ ! -f "$SRCDIR/Assembly-CSharp-firstpass.dll" ] \
   || [ ! -f "$SRCDIR/Assembly-CSharp.dll" ] \
   || [ ! -f "$SRCDIR/UnityEngine.dll" ];
then
   if [ "$TRAVIS_SECURE_ENV_VARS" = "false" ]; then
      # this should only happen for pull requests
      echo "Unable to build as the env vars have not been set. Can't decrypt the zip."
      exit 0; # can't decide if this should error
   fi

   if [[ ! -f dlls.zip ]]; then
      echo "Need to get dependency .dll's"
      wget -O dlls.zip "https://www.dropbox.com/s/ubryvrhp2mzkyvl/dll_1.05.zip?dl=1"
   fi
   
   if [ -z "$ZIPPASSWORD" ]; then
      if [ "$TRAVIS" = "true" ]; then
          echo "Password not set, on travis and DLL's missing, can't build";
          exit 1;
      else
          echo "Password required to decrypt the zip";
          unzip dlls.zip -d src/RemoteTech/ # this will prompt for a password
      fi
   else
      unzip -P "$ZIPPASSWORD" dlls.zip -d src/RemoteTech/
   fi
   
   rm -f dlls.zip
fi

cd src/RemoteTech && xbuild /p:Configuration=Release

