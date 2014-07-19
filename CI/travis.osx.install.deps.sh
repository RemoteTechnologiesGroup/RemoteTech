#!/bin/bash
set -ev

MONO_VERSION="3.4.0"

echo "Installing Mono ${MONO_VERSION}"
wget "http://download.mono-project.com/archive/${MONO_VERSION}/macos-10-x86/MonoFramework-MDK-${MONO_VERSION}.macos10.xamarin.x86.pkg"
sudo installer -pkg "MonoFramework-MDK-${MONO_VERSION}.macos10.xamarin.x86.pkg" -target /