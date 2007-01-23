require 'erb'
require 'type-mapper'

class SWIGRenderer
  def initialize(template_file, apply_binding)
    @file = template_file
    @binding = apply_binding
  end
  
  def render
    template = ERB.new(File.read(@file))
    template_results = template.result(@binding)
	template_results
  end
end

class Main
  def initialize
    @type_mapper = TypeMapper.new
  end
  
  def run(srcname)
    renderer = SWIGRenderer.new(srcname, binding)
    results = renderer.render
    write(srcname, results)
  end
  
  def write(srcname, results)
    destname = "#{File.dirname(srcname)}/#{File.basename(srcname, '.ri')}.i"
    destfile = File.open(destname,'w')
    destfile.puts results
    destfile.close
  end
end

if __FILE__ == $0
  Main.new.run ARGV.shift
end