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

# A custom writer that formats XML like Visual Studio:
# - Skips the first space. It just happens to be an extra space after the XML declaration.
# - Writes DOS line endings. Be sure to save as binary so that Ruby doesn't screw the endings!
class CustomOut
    def initialize(writer)
        @writer = writer
    end

    def <<(value)
        if value == " " && !@first_space
            @first_space = true
            return self
        end
        @writer << value.gsub("\n", "\r\n")
    end
end

class XMLFile
    include REXML

    def self.show_help(me)
        puts "Usage: ruby #{me} OPERATION XPATH [PARAMETER]"
        puts "Examples:"
        methods.each do |m|
            next unless m.to_s =~ /^help_example_(.*)/
            [send(m)].flatten.each {|tail| puts "  ruby #{me} #{$1} #{tail}"}
        end
    end

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
        new_file_path = nil
        Tempfile.open(["xml_file_save_temp", ".xml"]) do |f|
            f.binmode # Preserve DOS line endings
            new_file_path = f.path
            formatter.write(@file, CustomOut.new(f))
        end
        FileUtils.mv(new_file_path, @filepath)
    end

    def operate(args)
        method = "op_#{args[0]}"
        raise "Unknown operation #{args[0]}" unless respond_to? method
        output = send method, *args[1..-1]
        if output.nil?
            save
        else
            puts output
        end
    end

    def op_set(xpath, text_value)
        @file.elements.each(xpath) do |e|
            puts "#{e.xpath} = #{text_value}" if @verbose
            e.text = text_value
        end
        nil
    end

    def op_get(xpath)
        values = []
        @file.elements.each(xpath) {|e| values << e.text}
        values
    end
    
    def op_inc(xpath)
        @file.elements.each(xpath) do |e|
            next unless e.text =~ /[0-9]+/
            e.text = (e.text.to_i + 1).to_s
            puts "#{e.xpath} = #{e.text}" if @verbose
        end
        nil
    end

    def op_addchild(xpath, child_name)
        @file.elements.each(xpath) do |e|
            Element.new(child_name, e)
        end
        nil
    end

    def op_remove(xpath)
        @file.delete_element(xpath)
        nil
    end

    private

    def self.help_example_get; "//element" end
    def self.help_example_set; "//element value" end
    def self.help_example_inc; "//integralElement" end
    def self.help_example_addchild; "//element child" end
    def self.help_example_remove; "//element" end
end
