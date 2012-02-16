require 'english'
require 'pathname'
def include_dir; Pathname(__FILE__).expand_path.dirname end
require (include_dir + 'xml_file').to_s

class AWProjectFile < XMLFile

    def initialize; super(self.class.filepath) end

    def self.filepath
        (Pathname(__FILE__).dirname.parent + "AssaultWing" + "AssaultWing.csproj").realpath
    end
end

if __FILE__ == $PROGRAM_NAME
    if ARGV.length < 2
        puts "Usage:    ruby xml_file.rb [XPATH] [NEW_VALUE]"
        puts "Examples: ruby xml_file.rb //ApplicationRevision 42"
        puts "          ruby xml_file.rb //ApplicationVersion 1.69.0.%2a"
        puts "The config file is #{AWProjectFile.new.path}"
        exit
    end
    config = AWProjectFile.new
    config.set *ARGV
    config.save
end
