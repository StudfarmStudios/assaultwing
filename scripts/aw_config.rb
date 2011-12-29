require 'english'
require 'fileutils'
require 'pathname'
require 'tempfile'
require 'rexml/document'

class AWConfig
    include REXML

    def initialize(verbose = true)
        @verbose = verbose
        @config_file = find_config_file
        @config = Document.new(IO.read(@config_file))
    end

    def path; @config_file.to_s end

    def save
        formatter = Formatters::Pretty.new
        formatter.compact = true
        new_file = Tempfile.open(["aw_config", ".xml"])
        formatter.write(@config, new_file)
        new_file.close
        FileUtils.mv(new_file.path, @config_file)
    end

    def set(xpath, text_value)
        @config.elements.each(xpath) do |e|
            puts "#{e.xpath} = #{text_value}" if @verbose
            e.text = text_value
        end
    end

    private

    def find_config_file
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
