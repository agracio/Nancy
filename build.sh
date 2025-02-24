#!/usr/bin/env bash

# Define directories.
SCRIPT_DIR=$PWD
TOOLS_DIR=$SCRIPT_DIR/tools
CAKE_VERSION=1.3.0
CAKE_DLL=$TOOLS_DIR/Cake.$CAKE_VERSION/Cake.exe

# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

###########################################################################
# INSTALL CAKE
###########################################################################

if [ ! -f "$CAKE_DLL" ]; then
    curl -Lsfo Cake.zip "https://www.nuget.org/api/v2/package/Cake/$CAKE_VERSION" && unzip -q Cake.zip -d "$TOOLS_DIR/Cake.$CAKE_VERSION" && rm -f Cake.zip
    if [ $? -ne 0 ]; then
        echo "An error occured while installing Cake."
        exit 1
    fi
fi

# Make sure that Cake has been installed.
if [ ! -f "$CAKE_DLL" ]; then
    echo "Could not find Cake.exe at '$CAKE_DLL'."
    exit 1
fi

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Point net472 to Mono assemblies
export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/

# Start Cake
exec mono "$CAKE_DLL" "$@"
