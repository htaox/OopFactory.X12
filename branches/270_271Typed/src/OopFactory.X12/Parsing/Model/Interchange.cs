﻿using OopFactory.X12.Extensions;
using OopFactory.X12.Parsing.Model.Typed.Enums;
using OopFactory.X12.Parsing.Specification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace OopFactory.X12.Parsing.Model
{
    public class Interchange : Container, IXmlSerializable
    {
        private ISpecificationFinder _specFinder;
        private List<FunctionGroup> _functionGroups;

        internal Interchange() : base(null, null, "GS") { }

        internal Interchange(ISpecificationFinder specFinder, string segmentString)
            : base(null, new X12DelimiterSet(segmentString.ToCharArray()), segmentString)
        {
            _specFinder = specFinder;
            _functionGroups = new List<FunctionGroup>();
        }

        public Interchange(ISpecificationFinder specFinder, DateTime date, int controlNumber, bool production, X12DelimiterSet delimiters)
            : base(null, delimiters, String.Format("ISA{1}00{1}          {1}00{1}          {1}01{1}SENDERID HERE  {1}01{1}RECIEVERID HERE{1}{3:yyMMdd}{1}{3:HHmm}{1}U{1}00401{1}{4:000000000}{1}1{1}{5}{1}{2}{0}",
                delimiters.SegmentTerminator, delimiters.ElementSeparator, delimiters.SubElementSeparator, date, controlNumber, production ? "P" : "T"))
        {
            _specFinder = specFinder;
            if (controlNumber > 999999999 || controlNumber < 1)
                throw new ElementValidationException("{0} Interchange Control Number must be a positive number between 1 and 999999999.", "ISA00", controlNumber.ToString());

            _functionGroups = new List<FunctionGroup>();

            SetTerminatingTrailerSegment(String.Format("IEA{0}0{0}{2:000000000}{1}", delimiters.ElementSeparator, delimiters.SegmentTerminator, controlNumber));
        }

        public Interchange(DateTime date, int controlNumber, bool production)
            : this(new SpecificationFinder(), date, controlNumber, production, new X12DelimiterSet('~', '*', ':', '^'))
        {
        }

        public Interchange(DateTime date, int controlNumber, bool production, char segmentTerminator, char elementSeparator, char subElementSeparator, char? repetitionSeparator)
            : this(new SpecificationFinder(), date, controlNumber, production, new X12DelimiterSet(segmentTerminator, elementSeparator, subElementSeparator, repetitionSeparator))
        {
        }

        internal ISpecificationFinder SpecFinder
        {
            get { return _specFinder; }
        }
        public string AuthorInfoQualifier
        {
            get { return GetElement(1); }
            set { SetElement(1, String.Format("{0,-2}", value)); }
        }

        public string AuthorInfo
        {
            get { return GetElement(2); }
            set { SetElement(2, String.Format("{0,-10}", value)); }
        }

        public string SecurityInfoQualifier
        {
            get { return GetElement(3); }
            set { SetElement(3, String.Format("{0,-2}", value)); }
        }

        public string SecurityInfo
        {
            get { return GetElement(4); }
            set { SetElement(4, String.Format("{0,-10}", value)); }
        }

        public string InterchangeSenderIdQualifier
        {
            get { return GetElement(5); }
            set { SetElement(5, String.Format("{0,-2}", value)); }
        }

        public string InterchangeSenderId
        {
            get { return GetElement(6); }
            set { SetElement(6, String.Format("{0,-15}", value)); }
        }

        public string InterchangeReceiverIdQualifier
        {
            get { return GetElement(7); }
            set { SetElement(7, String.Format("{0,-2}", value)); }
        }

        public string InterchangeReceiverId
        {
            get { return GetElement(8); }
            set { SetElement(8, String.Format("{0,-15}", value)); }
        }

        public DateTime InterchangeDate
        {
            get
            {
                DateTime date;
                if (DateTime.TryParseExact(GetElement(9) + GetElement(10), "yyMMddHHmm", null, System.Globalization.DateTimeStyles.None, out date))
                    return date;
                else if (DateTime.TryParseExact(GetElement(9), "yyMMdd", null, System.Globalization.DateTimeStyles.None, out date))
                    return date;
                else
                    throw new ArgumentException(String.Format("{0} and {1} in ISA09 and ISA10 cannot be converted into a date and time.", GetElement(9), GetElement(10)));

            }
            set
            {
                SetElement(9, string.Format("{0:yyMMdd}", value));
                SetElement(10, string.Format("{0:HHmm}", value));
            }
        }

        public string InterchangeControlStandardRepetitionSeparator
        {
            get { return GetElement(11); }
            set { SetElement(11, String.Format("{0,-1}", value)); }
        }

        public string InterchangeControlVersionNumberCode
        {
            get { return GetElement(12); }
            set { SetElement(12, String.Format("{0,-5}", value)); }
        }

        public string InterchangeControlNumber
        {
            get { return GetElement(13); }
        }

        public string InterchangeAcknowledgmentRequestedCode
        {
            get { return GetElement(14); }
            set { SetElement(14, String.Format("{0,-1}", value)); }
        }

        public UsageIndicator InterchangeUsageIndicatorCode
        {
            get { return GetElement(15).ToEnumFromEDIFieldValue<UsageIndicator>(); }
        }

        public string InterchangeComponentElementSeparator
        {
            get { return GetElement(16); }
        }

        public IEnumerable<FunctionGroup> FunctionGroups
        {
            get { return _functionGroups; }
        }

        internal FunctionGroup AddFunctionGroup(string segmentString)
        {
            FunctionGroup fg = new FunctionGroup(_specFinder, this, _delimiters, segmentString);
            _functionGroups.Add(fg);
            return fg;
        }

        public FunctionGroup AddFunctionGroup(string functionIdCode, DateTime date, int controlNumber)
        {
            return AddFunctionGroup(functionIdCode, date, controlNumber, "004010X096A1");
        }

        public FunctionGroup AddFunctionGroup(string functionIdCode, DateTime date, int controlNumber, string version)
        {
            if (controlNumber > 999999999 || controlNumber < 1)
                throw new ElementValidationException("Element {0} cannot containe the value '{1}' because it must be a positive number between 1 and 999999999.",
                    "GS06", controlNumber.ToString());

            FunctionGroup fg = new FunctionGroup(_specFinder, this, _delimiters,
                string.Format("GS{0}{0}{0}{0}{0}{0}{0}X{0}{2}{1}", _delimiters.ElementSeparator, _delimiters.SegmentTerminator, version));
            fg.FunctionalIdentifierCode = functionIdCode;
            fg.Date = date;
            fg.ControlNumber = controlNumber;

            fg.SetTerminatingTrailerSegment(String.Format("GE{0}0{0}{2}{1}", _delimiters.ElementSeparator, _delimiters.SegmentTerminator, controlNumber));
            _functionGroups.Add(fg);
            return fg;
        }
        internal override IList<SegmentSpecification> AllowedChildSegments
        {
            get
            {
                var list = new List<SegmentSpecification>();
                return list;
            }
        }

        internal override IEnumerable<string> TrailerSegmentIds
        {
            get
            {
                return new List<string>();
            }
        }

        internal override string SerializeBodyToX12(bool addWhitespace)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var fg in _functionGroups)
                sb.Append(fg.ToX12String(addWhitespace));
            return sb.ToString();
        }

        internal override void SerializeBodyToX12(bool addWhitespace, System.IO.StreamWriter writer)
        {
            foreach (var fg in _functionGroups)
                fg.ToX12String(addWhitespace, writer);
        }

        internal override string ToX12String(bool addWhitespace)
        {
            UpdateTrailerSegmentCount(1, _functionGroups.Count);
            return base.ToX12String(addWhitespace);
        }

        internal override void ToX12String(bool addWhitespace, System.IO.StreamWriter writer)
        {
            UpdateTrailerSegmentCount(1, _functionGroups.Count);
            base.ToX12String(addWhitespace, writer);
        }

        public string Serialize()
        {
            return Serialize(false);
        }

        public virtual string Serialize(bool suppressComments)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Serialize(memoryStream, suppressComments);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(memoryStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public void Serialize(Stream stream, bool suppressComments = false)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
            var wrtr = suppressComments ? new NoCommentXmlTextWriter(stream, null) : new XmlTextWriter(stream, null);
            xmlSerializer.Serialize(wrtr, this);
        }

        #region IXmlSerializable Members

        internal override void WriteXml(XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(SegmentId))
            {
                UpdateTrailerSegmentCount(1, _functionGroups.Count);
                switch (_delimiters.SegmentTerminator)
                {
                    case '\x1D':
                        string terminator = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(_delimiters.SegmentTerminator.ToString()));
                        writer.WriteAttributeString("segment-terminator", terminator);
                        break;
                    default:
                        writer.WriteAttributeString("segment-terminator", _delimiters.SegmentTerminator.ToString());
                        break;
                }
                writer.WriteAttributeString("element-separator", _delimiters.ElementSeparator.ToString());
                writer.WriteAttributeString("sub-element-separator", _delimiters.SubElementSeparator.ToString());
                base.WriteXml(writer);

                foreach (var segment in this.Segments)
                    segment.WriteXml(writer);

                foreach (var functionGroup in this.FunctionGroups)
                    functionGroup.WriteXml(writer);

                foreach (var segment in this.TrailerSegments)
                    segment.WriteXml(writer);

            }
        }

        #endregion
    }
}
