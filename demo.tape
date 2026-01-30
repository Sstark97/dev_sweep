# demo.tape - DevSweep Demo
# 1. Run: ./setup-demo.sh to create demo files
# 2. Run: vhs demo.tape to record the demo
# 3. This demo shows analyze mode detecting ~14GB to clean

Output demo.gif

Set FontSize 14
Set Width 1400
Set Height 700
Set Padding 20
Set Theme "Dracula"

# Show help first
Type "# DevSweep - Professional macOS developer cache cleaner"
Sleep 1s
Enter
Enter
Sleep 500ms

Type "devsweep --help"
Sleep 500ms
Enter
Sleep 3s

# Clear screen
Ctrl+L
Sleep 500ms

# Run analyze mode to show what can be cleaned
Type "# Analyze mode - Preview what can be cleaned (~14GB)"
Sleep 1s
Enter
Sleep 500ms

Type "devsweep --analyze --all"
Sleep 500ms
Enter

# Wait for analysis to complete and show preview
Sleep 8s

# Show the preview clearly - answer no to not proceed
Type "n"
Enter
Sleep 2s

# Final pause to review the 13GB detected
Sleep 3s