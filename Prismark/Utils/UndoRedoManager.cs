using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prismark.Utils
{
    public class UndoRedoManager
    {
        private class UndoRedoInfo
        {
            public Stack<(string Text, int CaretPosition)> UndoStack { get; } = new Stack<(string, int)>();
            public Stack<(string Text, int CaretPosition)> RedoStack { get; } = new Stack<(string, int)>();
            public string CurrentText { get; set; } = "";
            public int CurrentCaretPosition { get; set; } = 0;
        }

        private Dictionary<string, UndoRedoInfo> fileUndoRedoInfo = new Dictionary<string, UndoRedoInfo>();
        private string currentFilePath;

        public void SetCurrentFile(string filePath)
        {
            currentFilePath = filePath;
            if (!fileUndoRedoInfo.ContainsKey(filePath))
            {
                fileUndoRedoInfo[filePath] = new UndoRedoInfo();
            }
        }

        public void SetInitialState(string initialText, int caretPosition)
        {
            if (currentFilePath == null) return;

            var info = fileUndoRedoInfo[currentFilePath];
            info.UndoStack.Clear();
            info.RedoStack.Clear();
            info.CurrentText = initialText;
            info.CurrentCaretPosition = caretPosition;
        }

        public void RecordState(string newText, int caretPosition)
        {
            if (currentFilePath == null) return;

            var info = fileUndoRedoInfo[currentFilePath];
            if (newText != info.CurrentText || caretPosition != info.CurrentCaretPosition)
            {
                info.UndoStack.Push((info.CurrentText, info.CurrentCaretPosition));
                info.CurrentText = newText;
                info.CurrentCaretPosition = caretPosition;
                info.RedoStack.Clear();
            }
        }

        public (string Text, int CaretPosition)? Undo()
        {
            if (currentFilePath == null) return null;

            var info = fileUndoRedoInfo[currentFilePath];
            if (info.UndoStack.Count > 0)
            {
                info.RedoStack.Push((info.CurrentText, info.CurrentCaretPosition));
                (info.CurrentText, info.CurrentCaretPosition) = info.UndoStack.Pop();
                return (info.CurrentText, info.CurrentCaretPosition);
            }
            return null;
        }

        public (string Text, int CaretPosition)? Redo()
        {
            if (currentFilePath == null) return null;

            var info = fileUndoRedoInfo[currentFilePath];
            if (info.RedoStack.Count > 0)
            {
                info.UndoStack.Push((info.CurrentText, info.CurrentCaretPosition));
                (info.CurrentText, info.CurrentCaretPosition) = info.RedoStack.Pop();
                return (info.CurrentText, info.CurrentCaretPosition);
            }
            return null;
        }

        public bool CanUndo()
        {
            return currentFilePath != null && fileUndoRedoInfo[currentFilePath].UndoStack.Count > 0;
        }

        public bool CanRedo()
        {
            return currentFilePath != null && fileUndoRedoInfo[currentFilePath].RedoStack.Count > 0;
        }
    }
}
