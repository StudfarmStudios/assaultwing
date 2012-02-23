require 'fileutils'
require 'pathname'
require 'tempfile'
require 'rexml/document'

# REXML::XMLDecl doesn't allow customizing its formatting to how Visual Studio writes XML.
class CustomXMLDecl < REXML::XMLDecl
    def to_s; "\xEF\xBB\xBF<?xml version=\"1.0\" encoding=\"utf-8\"?>" end
    def write(writer)
        writer << to_s
    end
end

# A writer that skips the first space. It just happens to be an extra space after the XML declaration.
# Visual Studio doesn't write it, so neither will we.
class CustomOut
    def initialize(writer)
        @writer = writer
    end

    def <<(value)
        if value == " " && !@first_space
            @first_space = true
            return self
        end
        @writer << value 
    end
end

class XMLFile
    include REXML

    def initialize(filepath, verbose = true)
        @verbose = verbose
        @filepath = filepath
        @file = Document.new(IO.read(@filepath), { :attribute_quote => :quote })
        @file << CustomXMLDecl.new
    end

    def path; @filepath.to_s.gsub("/", "\\") end

    def save
        formatter = Formatters::Pretty.new( 2, true ) # indent 2 and add a space before />
        formatter.compact = true
        new_file = Tempfile.open(["xml_file_save_temp", ".xml"])
        formatter.write(@file, CustomOut.new(new_file))
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
