require 'pathname'
def include_dir; Pathname(__FILE__).expand_path.dirname end
require (include_dir + 'aw_config').to_s

def bots_enabled; !Time.now.wednesday? || !(19..21).include?(Time.now.hour) end

config = AWConfig.new
config.set "//botsEnabled", bots_enabled
config.save
