// XTerm.js Terminal Integration for Blazor
window.xtermTerminal = {
    terminals: new Map(),

    // Initialize a new terminal instance
    create: function (elementId, dotNetObjectRef) {
        try {
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('Terminal element not found:', elementId);
                return false;
            }

            // Create terminal with configuration
            const terminal = new Terminal({
                cursorBlink: true,
                fontSize: 14,
                fontFamily: 'Consolas, "Courier New", monospace',
                theme: {
                    background: '#1e1e1e',
                    foreground: '#ffffff',
                    cursor: '#ffffff',
                    selection: '#3d3d3d'
                },
                allowTransparency: true,
                convertEol: true
            });

            // Create fit addon for responsive terminal
            const fitAddon = new FitAddon.FitAddon();
            terminal.loadAddon(fitAddon);

            // Open terminal in the DOM element
            terminal.open(element);
            fitAddon.fit();

            // Handle input
            terminal.onData(data => {
                try {
                    dotNetObjectRef.invokeMethodAsync('OnTerminalInput', data);
                } catch (error) {
                    console.error('Error sending input to .NET:', error);
                }
            });

            // Handle terminal resize
            terminal.onResize(({ cols, rows }) => {
                try {
                    dotNetObjectRef.invokeMethodAsync('OnTerminalResize', cols, rows);
                } catch (error) {
                    console.error('Error sending resize to .NET:', error);
                }
            });

            // Store terminal and addon for later use
            this.terminals.set(elementId, {
                terminal: terminal,
                fitAddon: fitAddon,
                dotNetRef: dotNetObjectRef
            });

            // Fit terminal on window resize
            const resizeHandler = () => {
                if (this.terminals.has(elementId)) {
                    fitAddon.fit();
                }
            };
            window.addEventListener('resize', resizeHandler);

            console.log('Terminal created successfully:', elementId);
            return true;
        } catch (error) {
            console.error('Error creating terminal:', error);
            return false;
        }
    },

    // Write text to terminal
    write: function (elementId, text) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData && terminalData.terminal) {
                terminalData.terminal.write(text);
                return true;
            }
            console.warn('Terminal not found for writing:', elementId);
            return false;
        } catch (error) {
            console.error('Error writing to terminal:', error);
            return false;
        }
    },

    // Clear terminal
    clear: function (elementId) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData && terminalData.terminal) {
                terminalData.terminal.clear();
                return true;
            }
            console.warn('Terminal not found for clearing:', elementId);
            return false;
        } catch (error) {
            console.error('Error clearing terminal:', error);
            return false;
        }
    },

    // Resize terminal to fit container
    fit: function (elementId) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData && terminalData.fitAddon) {
                terminalData.fitAddon.fit();
                return true;
            }
            console.warn('Terminal not found for fitting:', elementId);
            return false;
        } catch (error) {
            console.error('Error fitting terminal:', error);
            return false;
        }
    },

    // Focus terminal
    focus: function (elementId) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData && terminalData.terminal) {
                terminalData.terminal.focus();
                return true;
            }
            console.warn('Terminal not found for focusing:', elementId);
            return false;
        } catch (error) {
            console.error('Error focusing terminal:', error);
            return false;
        }
    },

    // Dispose terminal
    dispose: function (elementId) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData) {
                if (terminalData.terminal) {
                    terminalData.terminal.dispose();
                }
                if (terminalData.dotNetRef) {
                    terminalData.dotNetRef.dispose();
                }
                this.terminals.delete(elementId);
                console.log('Terminal disposed:', elementId);
                return true;
            }
            console.warn('Terminal not found for disposal:', elementId);
            return false;
        } catch (error) {
            console.error('Error disposing terminal:', error);
            return false;
        }
    },

    // Get terminal size
    getSize: function (elementId) {
        try {
            const terminalData = this.terminals.get(elementId);
            if (terminalData && terminalData.terminal) {
                return {
                    cols: terminalData.terminal.cols,
                    rows: terminalData.terminal.rows
                };
            }
            return null;
        } catch (error) {
            console.error('Error getting terminal size:', error);
            return null;
        }
    }
};