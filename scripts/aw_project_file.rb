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
    if ARGV.length < 1
        me = Pathname(__FILE__).basename
        puts "Usage:    ruby #{me} [XPATH] [NEW_VALUE]"
        puts "Sets NEW_VALUE if given, otherwise shows the current value."
        puts "Examples: ruby #{me} //ApplicationRevision"
        puts "          ruby #{me} //ApplicationRevision 42"
        puts "          ruby #{me} //ApplicationVersion 1.69.0.%2a"
        puts "The project file is #{AWProjectFile.new.path}"
        exit
    end
    config = AWProjectFile.new
    if ARGV.length < 2
        puts config.get(*ARGV)
        exit
    end
    config.set *ARGV
    config.save
end
