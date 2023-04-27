// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// FIXME: Is it possible to elegantly suppress StyleCop.CSharp.SpacingRules?
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.ContinuationRecord.RecordBlock")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.ContinuationRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.ConversionRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.ConversionRecord.RecordBlock")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.MetadataRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.PayloadTypeIdentifier.#ctor(System.Int32[])")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.PayloadTypeIdentifier.IndexOfPayload(System.Byte[],System.Int32[])~System.Int32")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.Record.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.RequestRecord.ContentBlock")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.RequestRecord.Payload")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.ResourceRecord.RecordBlock")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.ResponseRecord.ContentBlock")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~P:Toimik.WarcProtocol.ResponseRecord.Payload")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.ResponseRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.ResourceRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.RequestRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.RevisitRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.Utils.ConvertToBytes(System.String)~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.WarcinfoRecord.GetBlockBytes~System.Byte[]")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Analyzer's bug", Scope = "member", Target = "~M:Toimik.WarcProtocol.WarcWriter.WriteRecordToStream(Toimik.WarcProtocol.Record,System.IO.Stream)")]