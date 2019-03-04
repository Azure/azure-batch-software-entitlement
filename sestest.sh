#!/bin/bash

searchDir="`dirname "$0"`/out/sestest/"
sestest=$(find $searchDir -name "sestest.dll" -printf "%T@ %p\n" | sort -n | tail -1 | cut -f2- -d" ")

if [ -z "$sestest" ]
then
    echo "Could not find 'sestest.dll' within $searchDir"
    echo "Do you need to run a build?"
    exit -1
fi

dotnet $sestest "$@"
