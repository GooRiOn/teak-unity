import os
import shutil
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

new_dict = {'CFBundleTypeRole': 'Editor', 'CFBundleURLSchemes': ['teak' + appId]}
if "CFBundleURLTypes" in plist_data:
    plist_data["CFBundleURLTypes"].append(new_dict.copy())
else:
    plist_data["CFBundleURLTypes"] = [new_dict]

plistlib.writePlist(plist_data, os.path.join(path, 'Info.plist'))

project = XcodeProject.Load(path + '/Unity-iPhone.xcodeproj/project.pbxproj')

teak_cp_path = path + '/Teak/'
if not os.path.exists(teak_cp_path):
    os.makedirs(teak_cp_path)

print('Adding AdSupport.framework')
project.add_file_if_doesnt_exist('System/Library/Frameworks/AdSupport.framework', tree='SDKROOT')
print('Adding StoreKit.framework')
project.add_file_if_doesnt_exist('System/Library/Frameworks/StoreKit.framework', tree='SDKROOT')
print('Adding libsqlite3.tbd')
project.add_file_if_doesnt_exist('usr/lib/libsqlite3.tbd', tree='SDKROOT')

try:
    files_in_dir = os.listdir(fileToAddPath)
    for f in files_in_dir:
        if not f.startswith('.'): #ignore .DS_STORE
            pathname = os.path.join(fileToAddPath, f)
            fileName, fileExtension = os.path.splitext(pathname)
            if not fileExtension == '.meta': #ignore .meta as it is under asset server
                print('Adding ' + pathname + ' as ' + teak_cp_path + os.path.basename(pathname))
                if os.path.isfile(pathname):
                    shutil.copy2(pathname, teak_cp_path)
                    project.add_file_if_doesnt_exist(teak_cp_path + os.path.basename(pathname))
                if os.path.isdir(pathname):
                    shutil.copy2(pathname, teak_cp_path)
                    project.add_folder(teak_cp_path + os.path.basename(pathname), excludes=["^.*\.meta$"])
except OSError as e:
    # May want to check if e is actually no such file, and re-throw if not
    pass
finally:
    if project.modified:
        project.backup()
        project.save()
