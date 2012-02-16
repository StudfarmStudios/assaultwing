require 'fileutils'
require 'pathname'
require 'tempfile'
require 'rexml/document'

class XMLFile
    include REXML

    def initialize(filepath, verbose = true)
        @verbose = verbose
        @filepath = filepath
        @file = Document.new(IO.read(@filepath))
    end

    def path; @filepath.to_s.gsub("/", "\\") end

    def save
        formatter = Formatters::Pretty.new
        formatter.compact = true
        new_file = Tempfile.open(["xml_file_save_temp", ".xml"])
        formatter.write(@file, new_file)
        new_file.close
        FileUtils.mv(new_file.path, @filepath)
    end

    def set(xpath, text_value)
        @file.elements.each(xpath) do |e|
            puts "#{e.xpath} = #{text_value}" if @verbose
            e.text = text_value
        end
    end

    def get(xpath)
        values = []
        @file.elements.each(xpath) {|e| values << e.text}
        values
    end
    
    def increment(xpath)
        @file.elements.each(xpath) do |e|
            next unless e.text =~ /[0-9]+/
            e.text = (e.text.to_i + 1).to_s
            puts "#{e.xpath} = #{e.text}" if @verbose
        end
    end
end
