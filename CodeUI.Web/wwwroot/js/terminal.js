// XTerm.js Terminal Integration for Blazor
// Thin transport: forward all input to .NET and render only backend output.
window.xtermTerminal = {
    terminals: new Map(),

    // Initialize a new terminal instance
    create: function (elementId, dotNetObjectRef) {
        console.log('xtermTerminal.create called with elementId:', elementId);

        try {
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('Terminal element not found:', elementId);
                console.log('Available elements with terminal in ID:',
                    Array.from(document.querySelectorAll('[id*="terminal"]')).map(e => e.id));
                return false;
            }
            console.log('Found terminal element:', element.id);

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

            // Handle input: forward raw data to .NET; no local echo or buffering
            terminal.onData(data => {
                try {
                    if (dotNetObjectRef && typeof dotNetObjectRef.invokeMethodAsync === 'function') {
                        dotNetObjectRef.invokeMethodAsync('OnTerminalInput', data).catch(err => {
                            console.error('Failed to forward input to .NET:', err);
                        });
                    }
                } catch (error) {
                    console.error('Error forwarding terminal input:', error);
                }
            });

            // Handle terminal resize
            terminal.onResize(({ cols, rows }) => {
                try {
                    if (dotNetObjectRef && typeof dotNetObjectRef.invokeMethodAsync === 'function') {
                        dotNetObjectRef.invokeMethodAsync('OnTerminalResize', cols, rows).catch(err => {
                            console.error('Failed to forward resize to .NET:', err);
                        });
                    }
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
                    try { fitAddon.fit(); } catch {}
                }
            };
            window.addEventListener('resize', resizeHandler);

            // Focus only; content is written by backend
            terminal.focus();

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
                    try { terminalData.dotNetRef.dispose(); } catch {}
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

