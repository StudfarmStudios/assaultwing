require 'english'
require 'pathname'
def include_dir; Pathname(__FILE__).expand_path.dirname end
require (include_dir + 'xml_file').to_s

class AWConfig < XMLFile

    def self.each; filepaths.each {|filepath| yield new filepath} end

    def self.filepaths
        data_root = (Pathname(ENV["APPDATA"]) + ".." + "Local" + "Apps" + "2.0" + "Data").realpath
        config_files = Pathname.glob(data_root + "**" + "AssaultWing_config.xml").
            sort_by{|f| f.dirname.ctime}.
            map{|f| f.to_s}
        # Note: config_files may contain both a current and a previous version of the public AW and the developer AW.
        # To distinguish between current and previous, see directory creation date.
        # To distinguish between public and developer flavours, add some distinguishing file in the dirs at AW FirstRun.
        config_files
    end
end

if __FILE__ == $PROGRAM_NAME
    if ARGV.length < 2
        me = Pathname(__FILE__).basename
        puts "Usage:   ruby #{me} [XPATH] [NEW_VALUE]"
        puts "Example: ruby #{me} //botsEnabled false"
        puts "The config files are #{AWConfig.filepaths}"
        exit
    end
    AWConfig.each do |config|
        config.set *ARGV
        config.save
    end
end
