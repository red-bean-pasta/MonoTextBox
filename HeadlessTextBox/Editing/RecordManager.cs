using System.Diagnostics;
using System.Runtime.InteropServices;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Editing.Recording;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;

namespace HeadlessTextBox.Editing;

public class RecordManager: CeiledStack<Record>
{
    private readonly TextRecorder _textRecorder = new();
    private readonly FormatRecorder _formatRecorder = new();
    
    private readonly CeiledStack<Record> _redoStack;

    private int _pruneCounter = 0;


    public RecordManager(int capacity) : base(capacity)
    {
        _redoStack = new CeiledStack<Record>(capacity);
    }


    public void Undo(SourceBuffer storage)
    {
        if (!Pop(out var record))
            return;

        UndoRedoHelper.Undo(record, _textRecorder, _formatRecorder, storage);
        _redoStack.Add(record);
    }
    
    public void Redo(SourceBuffer storage)
    {
        if (!_redoStack.Pop(out var record))
            return;

        UndoRedoHelper.Redo(record, _textRecorder, _formatRecorder, storage);
        Add(record);
    }
    
    
    public void Insert(
        Caret caretBefore,
        ReadOnlySpan<char> text,
        IFormat format,
        bool forceNewRecord)
    {
        var formatPiece = new FormatPiece(format, text.Length);
        var formatSpan = MemoryMarshal.CreateSpan(ref formatPiece, 1);
        Insert(caretBefore, text, formatSpan, forceNewRecord);
    }
    
    public void Insert(
        Caret caretBefore,
        ReadOnlySpan<char> text,
        ReadOnlySpan<FormatPiece> format,
        bool forceNewRecord)
    {
        if (forceNewRecord || !CheckInsertMergeable()) 
            AddNewEmptyRecord(caretBefore);
        
        GetCurrentValue(out var record);
        var updatedRecord = record with
        {
            InsertedText = _textRecorder.ExtendInsert(caretBefore, text, record.InsertedText),
            AppliedFormat = _formatRecorder.ExtendApply(caretBefore.Left, format, record.AppliedFormat)
        };
        UpdateCurrentRecord(updatedRecord);
    }

    private bool CheckInsertMergeable()
    {
        if (!base.GetCurrentValue(out var last))
            return false;

        if (CheckIfFormatChangeOnly(last))
            return false;
        
        return true;
    }


    /// <summary>
    /// Distinguish with <see cref="Backspace"/>
    /// </summary>
    /// <param name="caretBefore"></param>
    /// <param name="textDeleted"></param>
    /// <param name="formatAttached"></param>
    /// <param name="forceNewRecord"></param>
    public void Delete(
        Caret caretBefore,
        ReadOnlySpan<char> textDeleted,
        ReadOnlySpan<FormatPiece> formatAttached,
        bool forceNewRecord)
    {
        if (forceNewRecord || !CheckDeleteMergeable()) 
            AddNewEmptyRecord(caretBefore);
        
        GetCurrentValue(out var record);
        var updatedRecord = record with
        {
            RemovedText = _textRecorder.ExtendDelete(caretBefore, textDeleted, record.RemovedText),
            RemovedFormat = _formatRecorder.ExtendRemove(caretBefore.Left, formatAttached, record.RemovedFormat)
        };
        UpdateCurrentRecord(updatedRecord);
    }

    private bool CheckDeleteMergeable()
    {
        if (!GetCurrentValue(out var last))
            return false;
        
        if (last.InsertedText.Count > 0)
            return false;

        if (CheckIfFormatChangeOnly(last))
            return false;

        return true;
    }
    
    
    public void Backspace(
        Caret caretBefore,
        ReadOnlySpan<char> textRemoved,
        ReadOnlySpan<FormatPiece> formatAttached,
        bool forceNewRecord)
    {
        if (forceNewRecord || !CheckBackspaceMergeable()) 
            AddNewEmptyRecord(caretBefore);
        
        GetCurrentValue(out var record);
        var updatedRecord = record with
        {
            RemovedText = _textRecorder.ExtendBackspace(caretBefore, textRemoved, record.RemovedText),
            RemovedFormat = _formatRecorder.ExtendBackspace(caretBefore.Left, formatAttached, record.RemovedFormat)
        };
        UpdateCurrentRecord(updatedRecord);
    }
    
    private bool CheckBackspaceMergeable()
    {
        return CheckDeleteMergeable();
    }


    public void Replace(
        Caret caretBefore,
        ReadOnlySpan<char> textDeleted,
        ReadOnlySpan<FormatPiece> formatDeleted,
        ReadOnlySpan<char> textInserted,
        IFormat formatInserted,
        bool forceNewRecord)
    {
        Delete(caretBefore, textDeleted, formatDeleted, forceNewRecord);
        Insert(caretBefore, textInserted, formatInserted, forceNewRecord);
    }
    
    public void Replace(
        Caret caretBefore,
        ReadOnlySpan<char> textDeleted,
        ReadOnlySpan<FormatPiece> formatDeleted,
        ReadOnlySpan<char> textInserted,
        ReadOnlySpan<FormatPiece> formatInserted,
        bool forceNewRecord)
    {
        Delete(caretBefore, textDeleted, formatDeleted, forceNewRecord);
        Insert(caretBefore, textInserted, formatInserted, forceNewRecord);
    }


    public void ChangeFormat(
        Caret caretBefore,
        ReadOnlySpan<FormatPiece> formatOverriden,
        IFormat formatApplied,
        bool forceNewRecord)
    {
        var piece = new FormatPiece(formatApplied, caretBefore.Length);
        var span = MemoryMarshal.CreateSpan(ref piece, 1);
        ChangeFormat(caretBefore, formatOverriden, span, forceNewRecord);
    }
    
    public void ChangeFormat(
        Caret caretBefore,
        ReadOnlySpan<FormatPiece> formatOverriden,
        ReadOnlySpan<FormatPiece> formatApplied,
        bool forceNewRecord)
    {
        if (forceNewRecord || !CheckFormatChangeMergeable()) 
            AddNewEmptyRecord(caretBefore);
        
        GetCurrentValue(out var record);
        var updatedRecord = record with
        {
            RemovedFormat = _formatRecorder.ExtendRemove(caretBefore.Left, formatOverriden, record.RemovedFormat),
            AppliedFormat = _formatRecorder.ExtendApply(caretBefore.Left, formatApplied, record.RemovedFormat)
        };
        UpdateCurrentRecord(updatedRecord);
    }
    
    private bool CheckFormatChangeMergeable()
    {
        if (!GetCurrentValue(out var last))
            return false;
        
        if (last.RemovedFormat.Count > 0 || last.InsertedText.Count > 0)
            return false;

        return true;
    }


    private void AddNewEmptyRecord(Caret caretBefore)
    {
        var (removedTextUnit, insertedTextUnit) = _textRecorder.GetNewUnits();
        var (removedFormatUnit, appliedFormatUnit) = _formatRecorder.GetNewUnits();
        var newRecord = new Record(caretBefore, removedTextUnit, removedFormatUnit, insertedTextUnit, appliedFormatUnit);
        Add(newRecord);
        
        CountAndPrune();
    }
    
    private void UpdateCurrentRecord(Record newRecord)
    {
        Items[CurrentIndex] = newRecord;
        
        _redoStack.Clear();
    }


    private void CountAndPrune()
    {
        _pruneCounter++;
        
        if (_pruneCounter <= Capacity)
            return;

        if (_pruneCounter % Capacity <= Capacity / 10)
            return;

        Prune();
        _pruneCounter = 0;
    }
    
    private void Prune()
    {
        Debug.Assert(GetFirstValue(out var first));
        _textRecorder.Prune(first.RemovedText);
        _formatRecorder.Prune(first.RemovedFormat);
    }

    private static bool CheckIfFormatChangeOnly(Record record)
    {
        if (record.InsertedText.Count > 0 || record.RemovedFormat.Count > 0)
            return false;

        if (record.RemovedFormat.Count > 0 || record.AppliedFormat.Count > 0)
            return true;

        return false;
    }
}