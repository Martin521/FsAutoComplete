module FsAutoComplete.CodeFix.ChangeDowncastToUpcast

open FsToolkit.ErrorHandling
open FsAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsAutoComplete
open FsAutoComplete.LspHelpers

let titleUpcastOperator = "Use ':>' operator"
let titleUpcastFunction = "Use 'upcast' function"

/// a codefix that replaces unsafe casts with safe casts
let fix (getRangeText: GetRangeText) : CodeFix =
  Run.ifDiagnosticByCode (Set.ofList [ "3198" ]) (fun diagnostic codeActionParams ->
    async {
      match! getRangeText (codeActionParams.TextDocument.GetFilePath() |> Utils.normalizePath) diagnostic.Range with
      | Ok expressionText ->
        let isDowncastOperator = expressionText.Contains(":?>")
        let isDowncastKeyword = expressionText.Contains("downcast")

        match isDowncastOperator, isDowncastKeyword with
        // must be either/or here, cannot be both
        | true, true -> return! AsyncResult.retn []
        | false, false -> return! AsyncResult.retn []
        | true, false ->
          return!
            AsyncResult.retn
              [ { File = codeActionParams.TextDocument
                  SourceDiagnostic = Some diagnostic
                  Title = titleUpcastOperator
                  Edits =
                    [| { Range = diagnostic.Range
                         NewText = expressionText.Replace(":?>", ":>") } |]
                  Kind = FixKind.Refactor } ]
        | false, true ->
          return!
            AsyncResult.retn
              [ { File = codeActionParams.TextDocument
                  SourceDiagnostic = Some diagnostic
                  Title = titleUpcastFunction
                  Edits =
                    [| { Range = diagnostic.Range
                         NewText = expressionText.Replace("downcast", "upcast") } |]
                  Kind = FixKind.Refactor } ]
      | Error _ -> return! AsyncResult.retn []
    })
