#!/bin/bash

PATH=/src/RemoteTech2

if [ ! -f "$PATH/Assembly-CSharp-firstpass.dll" ] \
   || [ ! -f "$PATH/Assembly-CSharp.dll" ] \
   || [ ! -f "$PATH/UnityEngine.dll" ];
then
   echo "Need to get dependency .dll's"
   wget "https://www.dropbox.com/s/kyv25p3qn166nzp/dlls.zip?dl=1"
   unzip dlls.zip -d src/RemoteTech2/
   rm -f dlls.zip
fi

cd src/RemoteTech2 && xbuild
