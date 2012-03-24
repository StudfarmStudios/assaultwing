require 'english'
require 'pathname'
def include_dir; Pathname(__FILE__).expand_path.dirname end
require (include_dir + 'xml_file').to_s

class AWProjectFile < XMLFile

    def initialize; super(self.class.filepath) end

    def self.filepath
        (Pathname(__FILE__).dirname.parent + "AssaultWing" + "AssaultWing.csproj").realpath
    end

    private

    def self.help_inserts_get; "//ApplicationRevision" end
    def self.help_inserts_set; ["//ApplicationRevision 42", "//ApplicationVersion 1.69.0.%2a"] end
    def self.help_inserts_inc; "//ApplicationRevision" end
end

if __FILE__ == $PROGRAM_NAME
    begin
        if ARGV.length == 0
            AWProjectFile.show_help Pathname(__FILE__).basename
            puts "The project file is #{AWProjectFile.new.path}"
        else
            AWProjectFile.new.operate ARGV
        end
    rescue Exception => e
        puts "Error! #{e}"
    end
end
