require 'english'
require 'pathname'
def include_dir; Pathname(__FILE__).expand_path.dirname end
require (include_dir + 'xml_file').to_s

class AWConfig < XMLFile

    private

    def find_filepath
        data_root = Pathname(ENV["APPDATA"]) + ".." + "Local" + "Apps" + "2.0"+ "Data"
        data_dirs = []
        data_root.find {|f| data_dirs << f if f.basename.to_s =~ /assa\.\.tion/ }
        latest_data_dir = data_dirs.sort{|d,e| e.ctime <=> d.ctime}.first
        latest_data_dir.find{|f| return f.to_s if f.basename.to_s == "AssaultWing_config.xml"}
        raise "No config file found"
    end
end

if __FILE__ == $PROGRAM_NAME
    if ARGV.length < 2
        puts "Usage:   ruby aw_config.rb [XPATH] [NEW_VALUE]"
        puts "Example: ruby aw_config.rb //botsEnabled false"
        puts "The config file is #{AWConfig.new.path}"
        exit
    end
    config = AWConfig.new
    config.set *ARGV
    config.save
end
