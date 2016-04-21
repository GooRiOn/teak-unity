require "rake/clean"
CLEAN.include "**/.DS_Store"

desc "Build Unity package"
task :default

#
# Helper methods
#
def unity(*args)
  # Run Unity.
  sh "/Applications/Unity/Unity.app/Contents/MacOS/Unity #{args.join(' ')}"
end

def unity?
  # Return true if we can run Unity.
  File.exist? "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
end

# Docs task
DOXYGEN_BINARY = "/Applications/Doxygen.app/Contents/Resources/doxygen"

def doxygen?
  return if not File.exist?(DOXYGEN_BINARY)
  return true
end

if doxygen?
  task :docs do
    sh DOXYGEN_BINARY
  end
end

def umv(src, dest)
  return if not File.exist? src
  mv src, dest
  mv "#{src}.meta", "#{dest}.meta"
end

#
# Unity build tasks
#

task :default => "unity:package"

desc "Build Unity Package"
task :unity => "unity:package"
namespace :unity do
  task :package do
    project_path = File.expand_path("./")
    puts project_path
    package_path = File.expand_path("./Teak.unitypackage")
    Dir.chdir("#{project_path}/Assets") do
      umv "Example", ".Example"
      umv "Facebook", ".Facebook"
      Dir.chdir("Teak") do
        umv "Resources", ".Resources"
      end
      Dir.chdir("Plugins/Android") do
        umv "AndroidManifest.xml", ".AndroidManifest.xml"
        umv "res", ".res"
        umv "facebook", ".facebook"
        umv "android-support-v4.jar", ".android-support-v4.jar"
        umv "bolts.jar", ".bolts.jar"
      end
    end
    begin
      unity "-quit -batchmode -projectPath #{project_path} -exportPackage Assets #{package_path}"
    rescue
      puts "Unity build failed."
    end
    Dir.chdir("#{project_path}/Assets") do
      umv ".Example", "Example"
      umv ".Facebook", "Facebook"
      Dir.chdir("Teak") do
        umv ".Resources", "Resources"
      end
      Dir.chdir("Plugins/Android") do
        umv ".AndroidManifest.xml", "AndroidManifest.xml"
        umv ".res", "res"
        umv ".facebook", "facebook"
        umv ".android-support-v4.jar", "android-support-v4.jar"
        umv ".bolts.jar", "bolts.jar"
      end
    end
  end
end
