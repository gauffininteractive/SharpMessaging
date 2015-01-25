using System.Runtime.CompilerServices;

namespace SharpMessaging.Persistence
{
    /// <summary>
    ///     A file based queue used to store messages that should be sent to each subscriber
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This implementation uses append-only files to store the messages that should be delivered. Each message is
    ///         stored using a data record format consisting of:
    ///         <list type="table">
    ///             <item>
    ///                 <term>STX</term>
    ///                 <definition>
    ///                     One byte: ASCII 2. Defines where a record starts. Used to be able to identify the next
    ///                     record if the current one is corrupted.
    ///                 </definition>
    ///             </item>
    ///             <item>
    ///                 <term>Record length</term>
    ///                 <definition>INT (4 bytes). Size of the serialized message</definition>
    ///             </item>
    ///             <item>
    ///                 <term>Record</term>
    ///                 <definition>JSON data (using UTF8 encoding)</definition>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [CompilerGenerated]
    internal class NamespaceDoc
    {
    }
}