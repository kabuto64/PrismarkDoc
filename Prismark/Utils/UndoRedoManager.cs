using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prismark.Utils
{
    public class UndoRedoManager
    {
        private const int MaxStackSize = 100;

        private class UndoRedoInfo
        {
            public LinkedList<(string Text, int CaretPosition)> UndoList { get; } = new LinkedList<(string, int)>();
            public LinkedList<(string Text, int CaretPosition)> RedoList { get; } = new LinkedList<(string, int)>();
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
            info.UndoList.Clear();
            info.RedoList.Clear();
            info.CurrentText = initialText;
            info.CurrentCaretPosition = caretPosition;
        }

        public void RecordState(string newText, int caretPosition)
        {
            if (currentFilePath == null) return;

            var info = fileUndoRedoInfo[currentFilePath];
            if (newText != info.CurrentText || caretPosition != info.CurrentCaretPosition)
            {
                info.UndoList.AddFirst((info.CurrentText, info.CurrentCaretPosition));
                if (info.UndoList.Count > MaxStackSize)
                {
                    info.UndoList.RemoveLast();
                }
                info.CurrentText = newText;
                info.CurrentCaretPosition = caretPosition;
                info.RedoList.Clear();
            }
        }

        public (string Text, int CaretPosition)? Undo()
        {
            if (currentFilePath == null) return null;

            var info = fileUndoRedoInfo[currentFilePath];
            if (info.UndoList.Count > 0)
            {
                info.RedoList.AddFirst((info.CurrentText, info.CurrentCaretPosition));
                if (info.RedoList.Count > MaxStackSize)
                {
                    info.RedoList.RemoveLast();
                }
                var state = info.UndoList.First.Value;
                info.UndoList.RemoveFirst();
                info.CurrentText = state.Text;
                info.CurrentCaretPosition = state.CaretPosition;
                return state;
            }
            return null;
        }

        public (string Text, int CaretPosition)? Redo()
        {
            if (currentFilePath == null) return null;

            var info = fileUndoRedoInfo[currentFilePath];
            if (info.RedoList.Count > 0)
            {
                info.UndoList.AddFirst((info.CurrentText, info.CurrentCaretPosition));
                if (info.UndoList.Count > MaxStackSize)
                {
                    info.UndoList.RemoveLast();
                }
                var state = info.RedoList.First.Value;
                info.RedoList.RemoveFirst();
                info.CurrentText = state.Text;
                info.CurrentCaretPosition = state.CaretPosition;
                return state;
            }
            return null;
        }

        public bool CanUndo()
        {
            return currentFilePath != null && fileUndoRedoInfo[currentFilePath].UndoList.Count > 0;
        }

        public bool CanRedo()
        {
            return currentFilePath != null && fileUndoRedoInfo[currentFilePath].RedoList.Count > 0;
        }
    }
}