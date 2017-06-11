require "rake/clean"
CLEAN.include "**/.DS_Store"

desc "Build Unity package"
task :default

UNITY_HOME="#{ENV['UNITY_HOME'] || '/Applications/Unity'}"

#
# Helper methods
#
def unity(*args)
  # Run Unity.
  sh "#{UNITY_HOME}/Unity.app/Contents/MacOS/Unity #{args.join(' ')}"
end

def unity?
  # Return true if we can run Unity.
  File.exist? "#{UNITY_HOME}/Unity.app/Contents/MacOS/Unity"
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

#
# Unity build tasks
#

task :default => "unity:package"

desc "Build Unity Package"
task :unity => "unity:package"
namespace :unity do
  task :package do
    project_path = File.expand_path("./")
    package_path = File.expand_path("./Teak.unitypackage")
    begin
      unity "-quit -batchmode -nographics -projectPath #{project_path} -executeMethod TeakPackageBuilder.BuildUnityPackage" do |ok, status|
        ok or fail "Unity build failed #{`cat ~/Library/Logs/Unity/Editor.log`}"
      end
      puts "#{`cat ~/Library/Logs/Unity/Editor.log`}"
      sh "python extractunitypackage.py Teak.unitypackage _temp_pkg/"
      FileUtils.rm_rf("_temp_pkg")
    rescue => error
      puts "Unity build failed: #{error}"
    end
  end
end

desc "Build Native Android Unity Library"
task :android => "android:build"
namespace :android do
  task :build do
    begin
      Dir.chdir('AndroidLibBuild') do
        sh "ant" do |ok, status|
          ok or fail "AndroidLibBuild failed"
        end
      end
    rescue => error
      puts "Native Android Unity Library build failed: #{error}"
    end
  end
end

desc "Build Native iOS Unity Library"
task :ios => "ios:build"
namespace :ios do
  task :build do
    begin
      Dir.chdir('iOSLibBuild') do
        sh "ant" do |ok, status|
          ok or fail "iOSLibBuild failed"
        end
      end
    rescue => error
      puts "Native iOS Unity Library build failed: #{error}"
    end
  end
end
