using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleReaderWriter
{
    [Serializable]
    public class Messages
    {
        #region Neutral/system messages

        /// <summary>
        /// Marker class to continue processing.
        /// </summary>
        [Serializable]
        public class ContinueProcessing { }

        #endregion

        #region Success messages

        /// <summary>
        /// Base class for signalling that user input was valid.
        /// </summary>
        [Serializable]
        public class InputSuccess
        {
            public InputSuccess(string reason)
            {
                Reason = reason;
            }

            public string Reason { get; private set; }
        }

        #endregion

        #region Error messages

        /// <summary>
        /// Base class for signalling that user input was invalid.
        /// </summary>
        [Serializable]
        public class InputError
        {
            public InputError(string reason)
            {
                Reason = reason;
            }

            public string Reason { get; private set; }
        }

        /// <summary>
        /// User provided blank input.
        /// </summary>
        [Serializable]
        public class NullInputError : InputError
        {
            public NullInputError(string reason) : base(reason) { }
        }

        /// <summary>
        /// User provided invalid input (currently, input w/ odd # chars)
        /// </summary>
        [Serializable]
        public class ValidationError : InputError
        {
            public ValidationError(string reason) : base(reason) { }
        }

        #endregion
    }
}
