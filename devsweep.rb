# Formula for DevSweep
# To test locally:
#   brew install --build-from-source ./devsweep.rb
# To publish:
#   1. Create a GitHub release with a tarball
#   2. Update the url and sha256 below
#   3. Submit a PR to homebrew-core or create your own tap

class Devsweep < Formula
  desc "Intelligent macOS developer cache cleaner"
  homepage "https://github.com/YOUR_USERNAME/dev_sweep"
  url "https://github.com/YOUR_USERNAME/dev_sweep/archive/v1.0.0.tar.gz"
  sha256 "REPLACE_WITH_ACTUAL_SHA256"
  license "MIT"
  head "https://github.com/YOUR_USERNAME/dev_sweep.git", branch: "main"

  depends_on "bash" => :build

  def install
    # Install all source files
    prefix.install "src"
    prefix.install "bin"
    
    # Create executable
    bin.install_symlink prefix/"bin/devsweep"
  end

  test do
    system "#{bin}/devsweep", "--version"
    system "#{bin}/devsweep", "--dry-run", "--jetbrains"
  end
end
