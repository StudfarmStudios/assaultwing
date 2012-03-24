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
        puts_example "  ruby #{me} get", help_inserts_get
        puts_example "  ruby #{me} set", help_inserts_set
        puts_example "  ruby #{me} inc", help_inserts_inc
        puts_example "  ruby #{me} addchild", help_inserts_addchild
        puts_example "  ruby #{me} remove", help_inserts_remove
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
        must_save = true
        args = args.clone
        operation = args.shift
        case operation
        when "get" then puts get(*args); must_save = false
        when "set" then set *args
        when "inc" then increment *args
        when "addchild" then add_child *args
        when "remove" then remove *args
        else raise "Unknown operation #{operation}"
        end
        save if must_save
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

    def add_child(xpath, child_name)
        @file.elements.each(xpath) do |e|
            Element.new(child_name, e)
        end
    end

    def remove(xpath)
        @file.delete_element(xpath)
    end

    private

    def self.puts_example(head, tails)
        [tails].flatten.each {|tail| puts "#{head} #{tail}"}
    end

    def self.help_inserts_get; "//element" end
    def self.help_inserts_set; "//element value" end
    def self.help_inserts_inc; "//integralElement" end
    def self.help_inserts_addchild; "//element child" end
    def self.help_inserts_remove; "//element" end
end
