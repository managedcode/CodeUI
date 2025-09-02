// Monaco Diff Editor Integration for Blazor
window.monacoDiffEditor = {
    editors: new Map(),

    // Initialize Monaco Editor environment
    initialize: function () {
        return new Promise((resolve, reject) => {
            try {
                if (typeof monaco !== 'undefined') {
                    resolve(true);
                    return;
                }

                require.config({ 
                    paths: { 
                        'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' 
                    } 
                });

                require(['vs/editor/editor.main'], function () {
                    // Set Monaco theme
                    monaco.editor.defineTheme('custom-dark', {
                        base: 'vs-dark',
                        inherit: true,
                        rules: [],
                        colors: {
                            'editor.background': '#1e1e1e',
                            'diffEditor.insertedTextBackground': '#1e7022',
                            'diffEditor.removedTextBackground': '#70222e',
                            'diffEditor.insertedLineBackground': '#1e7022',
                            'diffEditor.removedLineBackground': '#70222e'
                        }
                    });

                    console.log('Monaco Editor initialized successfully');
                    resolve(true);
                });
            } catch (error) {
                console.error('Error initializing Monaco Editor:', error);
                reject(error);
            }
        });
    },

    // Create a diff editor instance
    createDiffEditor: function (elementId, options, dotNetObjectRef) {
        try {
            const element = document.getElementById(elementId);
            if (!element) {
                console.error('Diff editor element not found:', elementId);
                return false;
            }

            const defaultOptions = {
                theme: 'custom-dark',
                readOnly: options.readOnly || false,
                renderSideBySide: options.renderSideBySide !== false,
                ignoreTrimWhitespace: false,
                renderIndicators: true,
                originalEditable: false,
                modifiedEditable: !options.readOnly,
                automaticLayout: true,
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                lineNumbers: 'on',
                glyphMargin: true,
                folding: true,
                selectOnLineNumbers: true,
                scrollbar: {
                    verticalScrollbarSize: 10,
                    horizontalScrollbarSize: 10
                }
            };

            const mergedOptions = Object.assign({}, defaultOptions, options);
            
            const diffEditor = monaco.editor.createDiffEditor(element, mergedOptions);

            // Store editor reference
            this.editors.set(elementId, {
                editor: diffEditor,
                dotNetRef: dotNetObjectRef,
                options: mergedOptions
            });

            // Handle resize
            const resizeObserver = new ResizeObserver(() => {
                diffEditor.layout();
            });
            resizeObserver.observe(element);

            // Handle line click events for accept/reject functionality
            const modifiedEditor = diffEditor.getModifiedEditor();
            modifiedEditor.onMouseDown((e) => {
                try {
                    if (e.target.type === monaco.editor.MouseTargetType.GUTTER_LINE_NUMBERS) {
                        const lineNumber = e.target.position.lineNumber;
                        dotNetObjectRef.invokeMethodAsync('OnLineClicked', lineNumber);
                    }
                } catch (error) {
                    console.error('Error handling line click:', error);
                }
            });

            console.log('Diff editor created successfully:', elementId);
            return true;
        } catch (error) {
            console.error('Error creating diff editor:', error);
            return false;
        }
    },

    // Set the content for both original and modified editors
    setContent: function (elementId, originalContent, modifiedContent, language) {
        try {
            const editorData = this.editors.get(elementId);
            if (!editorData || !editorData.editor) {
                console.warn('Diff editor not found:', elementId);
                return false;
            }

            const originalModel = monaco.editor.createModel(originalContent || '', language || 'plaintext');
            const modifiedModel = monaco.editor.createModel(modifiedContent || '', language || 'plaintext');

            editorData.editor.setModel({
                original: originalModel,
                modified: modifiedModel
            });

            return true;
        } catch (error) {
            console.error('Error setting diff editor content:', error);
            return false;
        }
    },

    // Update view mode (side-by-side vs inline)
    setViewMode: function (elementId, sideBySide) {
        try {
            const editorData = this.editors.get(elementId);
            if (!editorData || !editorData.editor) {
                console.warn('Diff editor not found:', elementId);
                return false;
            }

            editorData.editor.updateOptions({
                renderSideBySide: sideBySide
            });

            return true;
        } catch (error) {
            console.error('Error setting view mode:', error);
            return false;
        }
    },

    // Add decorations to highlight accepted/rejected lines
    setLineDecorations: function (elementId, decorations) {
        try {
            const editorData = this.editors.get(elementId);
            if (!editorData || !editorData.editor) {
                console.warn('Diff editor not found:', elementId);
                return false;
            }

            const modifiedEditor = editorData.editor.getModifiedEditor();
            
            const monacoDecorations = decorations.map(decoration => ({
                range: new monaco.Range(decoration.lineNumber, 1, decoration.lineNumber, 1),
                options: {
                    isWholeLine: true,
                    className: decoration.className,
                    glyphMarginClassName: decoration.glyphMarginClassName,
                    glyphMarginHoverMessage: { value: decoration.hoverMessage || '' }
                }
            }));

            modifiedEditor.deltaDecorations([], monacoDecorations);
            return true;
        } catch (error) {
            console.error('Error setting line decorations:', error);
            return false;
        }
    },

    // Resize the editor to fit its container
    layout: function (elementId) {
        try {
            const editorData = this.editors.get(elementId);
            if (editorData && editorData.editor) {
                editorData.editor.layout();
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error laying out diff editor:', error);
            return false;
        }
    },

    // Dispose of the editor and clean up resources
    dispose: function (elementId) {
        try {
            const editorData = this.editors.get(elementId);
            if (editorData) {
                if (editorData.editor) {
                    editorData.editor.dispose();
                }
                if (editorData.dotNetRef) {
                    editorData.dotNetRef.dispose();
                }
                this.editors.delete(elementId);
                console.log('Diff editor disposed:', elementId);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error disposing diff editor:', error);
            return false;
        }
    }
};

// Initialize Monaco when the script loads
document.addEventListener('DOMContentLoaded', function () {
    window.monacoDiffEditor.initialize().catch(console.error);
});