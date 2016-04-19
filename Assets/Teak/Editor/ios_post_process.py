import os
from sys import argv
from mod_pbxproj import XcodeProject
import plistlib

path = argv[1]
fileToAddPath = argv[2]
appId = argv[3]
apiKey = argv[4]

print('Adding Teak App Id and Api Key entries to Info.plist')
plist_data = plistlib.readPlist(os.path.join(path, 'Info.plist'))
plist_data["TeakAppId"] = appId
plist_data["TeakApiKey"] = apiKey
plistlib.writePlist(plist_data, os.path.join(path, 'Info.plist'))

project = XcodeProject.Load(path +'/Unity-iPhone.xcodeproj/project.pbxproj')

print('Adding AdSupport.framework')
project.add_file_if_doesnt_exist('System/Library/Frameworks/AdSupport.framework', tree='SDKROOT')
print('Adding libsqlite3.tbd')
project.add_file_if_doesnt_exist('usr/lib/libsqlite3.tbd', tree='SDKROOT')

files_in_dir = os.listdir(fileToAddPath)
for f in files_in_dir:    
    if not f.startswith('.'): #ignore .DS_STORE
        pathname = os.path.join(fileToAddPath, f)
        fileName, fileExtension = os.path.splitext(pathname)
        if not fileExtension == '.meta': #ignore .meta as it is under asset server
            print('Adding ' + pathname)
            if os.path.isfile(pathname):
                project.add_file_if_doesnt_exist(pathname)
            if os.path.isdir(pathname):
                project.add_folder(pathname, excludes=["^.*\.meta$"])

if project.modified:
    project.backup()
    project.save()
