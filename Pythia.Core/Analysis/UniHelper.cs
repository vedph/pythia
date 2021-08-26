﻿using Fusi.Text.Unicode;
using System.Collections.Generic;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// Unicode helper class.
    /// </summary>
    public static class UniHelper
    {
        #region Segments
        // mappings for getting segments
        // generated from Fusi.Wpf.Demo, segment table builder, range 0000-03FF, Unicode 6.2.
        private static readonly Dictionary<char, char> _segments = new Dictionary<char, char>
        {
        {'\u00c0', '\u0041'}, {'\u00c1', '\u0041'}, {'\u00c2', '\u0041'}, {'\u00c3', '\u0041'},
        {'\u00c4', '\u0041'}, {'\u00c5', '\u0041'}, {'\u00c7', '\u0043'}, {'\u00c8', '\u0045'},
        {'\u00c9', '\u0045'}, {'\u00ca', '\u0045'}, {'\u00cb', '\u0045'}, {'\u00cc', '\u0049'},
        {'\u00cd', '\u0049'}, {'\u00ce', '\u0049'}, {'\u00cf', '\u0049'}, {'\u00d1', '\u004e'},
        {'\u00d2', '\u004f'}, {'\u00d3', '\u004f'}, {'\u00d4', '\u004f'}, {'\u00d5', '\u004f'},
        {'\u00d6', '\u004f'}, {'\u00d9', '\u0055'}, {'\u00da', '\u0055'}, {'\u00db', '\u0055'},
        {'\u00dc', '\u0055'}, {'\u00dd', '\u0059'}, {'\u00e0', '\u0061'}, {'\u00e1', '\u0061'},
        {'\u00e2', '\u0061'}, {'\u00e3', '\u0061'}, {'\u00e4', '\u0061'}, {'\u00e5', '\u0061'},
        {'\u00e7', '\u0063'}, {'\u00e8', '\u0065'}, {'\u00e9', '\u0065'}, {'\u00ea', '\u0065'},
        {'\u00eb', '\u0065'}, {'\u00ec', '\u0069'}, {'\u00ed', '\u0069'}, {'\u00ee', '\u0069'},
        {'\u00ef', '\u0069'}, {'\u00f1', '\u006e'}, {'\u00f2', '\u006f'}, {'\u00f3', '\u006f'},
        {'\u00f4', '\u006f'}, {'\u00f5', '\u006f'}, {'\u00f6', '\u006f'}, {'\u00f9', '\u0075'},
        {'\u00fa', '\u0075'}, {'\u00fb', '\u0075'}, {'\u00fc', '\u0075'}, {'\u00fd', '\u0079'},
        {'\u00ff', '\u0079'}, {'\u0100', '\u0041'}, {'\u0101', '\u0061'}, {'\u0102', '\u0041'},
        {'\u0103', '\u0061'}, {'\u0104', '\u0041'}, {'\u0105', '\u0061'}, {'\u0106', '\u0043'},
        {'\u0107', '\u0063'}, {'\u0108', '\u0043'}, {'\u0109', '\u0063'}, {'\u010a', '\u0043'},
        {'\u010b', '\u0063'}, {'\u010c', '\u0043'}, {'\u010d', '\u0063'}, {'\u010e', '\u0044'},
        {'\u010f', '\u0064'}, {'\u0112', '\u0045'}, {'\u0113', '\u0065'}, {'\u0114', '\u0045'},
        {'\u0115', '\u0065'}, {'\u0116', '\u0045'}, {'\u0117', '\u0065'}, {'\u0118', '\u0045'},
        {'\u0119', '\u0065'}, {'\u011a', '\u0045'}, {'\u011b', '\u0065'}, {'\u011c', '\u0047'},
        {'\u011d', '\u0067'}, {'\u011e', '\u0047'}, {'\u011f', '\u0067'}, {'\u0120', '\u0047'},
        {'\u0121', '\u0067'}, {'\u0122', '\u0047'}, {'\u0123', '\u0067'}, {'\u0124', '\u0048'},
        {'\u0125', '\u0068'}, {'\u0128', '\u0049'}, {'\u0129', '\u0069'}, {'\u012a', '\u0049'},
        {'\u012b', '\u0069'}, {'\u012c', '\u0049'}, {'\u012d', '\u0069'}, {'\u012e', '\u0049'},
        {'\u012f', '\u0069'}, {'\u0130', '\u0049'}, {'\u0134', '\u004a'}, {'\u0135', '\u006a'},
        {'\u0136', '\u004b'}, {'\u0137', '\u006b'}, {'\u0139', '\u004c'}, {'\u013a', '\u006c'},
        {'\u013b', '\u004c'}, {'\u013c', '\u006c'}, {'\u013d', '\u004c'}, {'\u013e', '\u006c'},
        {'\u0143', '\u004e'}, {'\u0144', '\u006e'}, {'\u0145', '\u004e'}, {'\u0146', '\u006e'},
        {'\u0147', '\u004e'}, {'\u0148', '\u006e'}, {'\u014c', '\u004f'}, {'\u014d', '\u006f'},
        {'\u014e', '\u004f'}, {'\u014f', '\u006f'}, {'\u0150', '\u004f'}, {'\u0151', '\u006f'},
        {'\u0154', '\u0052'}, {'\u0155', '\u0072'}, {'\u0156', '\u0052'}, {'\u0157', '\u0072'},
        {'\u0158', '\u0052'}, {'\u0159', '\u0072'}, {'\u015a', '\u0053'}, {'\u015b', '\u0073'},
        {'\u015c', '\u0053'}, {'\u015d', '\u0073'}, {'\u015e', '\u0053'}, {'\u015f', '\u0073'},
        {'\u0160', '\u0053'}, {'\u0161', '\u0073'}, {'\u0162', '\u0054'}, {'\u0163', '\u0074'},
        {'\u0164', '\u0054'}, {'\u0165', '\u0074'}, {'\u0168', '\u0055'}, {'\u0169', '\u0075'},
        {'\u016a', '\u0055'}, {'\u016b', '\u0075'}, {'\u016c', '\u0055'}, {'\u016d', '\u0075'},
        {'\u016e', '\u0055'}, {'\u016f', '\u0075'}, {'\u0170', '\u0055'}, {'\u0171', '\u0075'},
        {'\u0172', '\u0055'}, {'\u0173', '\u0075'}, {'\u0174', '\u0057'}, {'\u0175', '\u0077'},
        {'\u0176', '\u0059'}, {'\u0177', '\u0079'}, {'\u0178', '\u0059'}, {'\u0179', '\u005a'},
        {'\u017a', '\u007a'}, {'\u017b', '\u005a'}, {'\u017c', '\u007a'}, {'\u017d', '\u005a'},
        {'\u017e', '\u007a'}, {'\u01a0', '\u004f'}, {'\u01a1', '\u006f'}, {'\u01af', '\u0055'},
        {'\u01b0', '\u0075'}, {'\u01cd', '\u0041'}, {'\u01ce', '\u0061'}, {'\u01cf', '\u0049'},
        {'\u01d0', '\u0069'}, {'\u01d1', '\u004f'}, {'\u01d2', '\u006f'}, {'\u01d3', '\u0055'},
        {'\u01d4', '\u0075'}, {'\u01d5', '\u0055'}, {'\u01d6', '\u0075'}, {'\u01d7', '\u0055'},
        {'\u01d8', '\u0075'}, {'\u01d9', '\u0055'}, {'\u01da', '\u0075'}, {'\u01db', '\u0055'},
        {'\u01dc', '\u0075'}, {'\u01de', '\u0041'}, {'\u01df', '\u0061'}, {'\u01e0', '\u0041'},
        {'\u01e1', '\u0061'}, {'\u01e2', '\u00c6'}, {'\u01e3', '\u00e6'}, {'\u01e6', '\u0047'},
        {'\u01e7', '\u0067'}, {'\u01e8', '\u004b'}, {'\u01e9', '\u006b'}, {'\u01ea', '\u004f'},
        {'\u01eb', '\u006f'}, {'\u01ec', '\u004f'}, {'\u01ed', '\u006f'}, {'\u01ee', '\u01b7'},
        {'\u01ef', '\u0292'}, {'\u01f0', '\u006a'}, {'\u01f4', '\u0047'}, {'\u01f5', '\u0067'},
        {'\u01f8', '\u004e'}, {'\u01f9', '\u006e'}, {'\u01fa', '\u0041'}, {'\u01fb', '\u0061'},
        {'\u01fc', '\u00c6'}, {'\u01fd', '\u00e6'}, {'\u01fe', '\u00d8'}, {'\u01ff', '\u00f8'},
        {'\u0200', '\u0041'}, {'\u0201', '\u0061'}, {'\u0202', '\u0041'}, {'\u0203', '\u0061'},
        {'\u0204', '\u0045'}, {'\u0205', '\u0065'}, {'\u0206', '\u0045'}, {'\u0207', '\u0065'},
        {'\u0208', '\u0049'}, {'\u0209', '\u0069'}, {'\u020a', '\u0049'}, {'\u020b', '\u0069'},
        {'\u020c', '\u004f'}, {'\u020d', '\u006f'}, {'\u020e', '\u004f'}, {'\u020f', '\u006f'},
        {'\u0210', '\u0052'}, {'\u0211', '\u0072'}, {'\u0212', '\u0052'}, {'\u0213', '\u0072'},
        {'\u0214', '\u0055'}, {'\u0215', '\u0075'}, {'\u0216', '\u0055'}, {'\u0217', '\u0075'},
        {'\u0218', '\u0053'}, {'\u0219', '\u0073'}, {'\u021a', '\u0054'}, {'\u021b', '\u0074'},
        {'\u021e', '\u0048'}, {'\u021f', '\u0068'}, {'\u0226', '\u0041'}, {'\u0227', '\u0061'},
        {'\u0228', '\u0045'}, {'\u0229', '\u0065'}, {'\u022a', '\u004f'}, {'\u022b', '\u006f'},
        {'\u022c', '\u004f'}, {'\u022d', '\u006f'}, {'\u022e', '\u004f'}, {'\u022f', '\u006f'},
        {'\u0230', '\u004f'}, {'\u0231', '\u006f'}, {'\u0232', '\u0059'}, {'\u0233', '\u0079'},
        {'\u0300', '\u0000'}, {'\u0301', '\u0000'}, {'\u0302', '\u0000'}, {'\u0303', '\u0000'},
        {'\u0304', '\u0000'}, {'\u0305', '\u0000'}, {'\u0306', '\u0000'}, {'\u0307', '\u0000'},
        {'\u0308', '\u0000'}, {'\u0309', '\u0000'}, {'\u030a', '\u0000'}, {'\u030b', '\u0000'},
        {'\u030c', '\u0000'}, {'\u030d', '\u0000'}, {'\u030e', '\u0000'}, {'\u030f', '\u0000'},
        {'\u0310', '\u0000'}, {'\u0311', '\u0000'}, {'\u0312', '\u0000'}, {'\u0313', '\u0000'},
        {'\u0314', '\u0000'}, {'\u0315', '\u0000'}, {'\u0316', '\u0000'}, {'\u0317', '\u0000'},
        {'\u0318', '\u0000'}, {'\u0319', '\u0000'}, {'\u031a', '\u0000'}, {'\u031b', '\u0000'},
        {'\u031c', '\u0000'}, {'\u031d', '\u0000'}, {'\u031e', '\u0000'}, {'\u031f', '\u0000'},
        {'\u0320', '\u0000'}, {'\u0321', '\u0000'}, {'\u0322', '\u0000'}, {'\u0323', '\u0000'},
        {'\u0324', '\u0000'}, {'\u0325', '\u0000'}, {'\u0326', '\u0000'}, {'\u0327', '\u0000'},
        {'\u0328', '\u0000'}, {'\u0329', '\u0000'}, {'\u032a', '\u0000'}, {'\u032b', '\u0000'},
        {'\u032c', '\u0000'}, {'\u032d', '\u0000'}, {'\u032e', '\u0000'}, {'\u032f', '\u0000'},
        {'\u0330', '\u0000'}, {'\u0331', '\u0000'}, {'\u0332', '\u0000'}, {'\u0333', '\u0000'},
        {'\u0334', '\u0000'}, {'\u0335', '\u0000'}, {'\u0336', '\u0000'}, {'\u0337', '\u0000'},
        {'\u0338', '\u0000'}, {'\u0339', '\u0000'}, {'\u033a', '\u0000'}, {'\u033b', '\u0000'},
        {'\u033c', '\u0000'}, {'\u033d', '\u0000'}, {'\u033e', '\u0000'}, {'\u033f', '\u0000'},
        {'\u0340', '\u0000'}, {'\u0341', '\u0000'}, {'\u0342', '\u0000'}, {'\u0343', '\u0000'},
        {'\u0344', '\u0000'}, {'\u0345', '\u0000'}, {'\u0346', '\u0000'}, {'\u0347', '\u0000'},
        {'\u0348', '\u0000'}, {'\u0349', '\u0000'}, {'\u034a', '\u0000'}, {'\u034b', '\u0000'},
        {'\u034c', '\u0000'}, {'\u034d', '\u0000'}, {'\u034e', '\u0000'}, {'\u034f', '\u0000'},
        {'\u0350', '\u0000'}, {'\u0351', '\u0000'}, {'\u0352', '\u0000'}, {'\u0353', '\u0000'},
        {'\u0354', '\u0000'}, {'\u0355', '\u0000'}, {'\u0356', '\u0000'}, {'\u0357', '\u0000'},
        {'\u0358', '\u0000'}, {'\u0359', '\u0000'}, {'\u035a', '\u0000'}, {'\u035b', '\u0000'},
        {'\u035c', '\u0000'}, {'\u035d', '\u0000'}, {'\u035e', '\u0000'}, {'\u035f', '\u0000'},
        {'\u0360', '\u0000'}, {'\u0361', '\u0000'}, {'\u0362', '\u0000'}, {'\u0363', '\u0000'},
        {'\u0364', '\u0000'}, {'\u0365', '\u0000'}, {'\u0366', '\u0000'}, {'\u0367', '\u0000'},
        {'\u0368', '\u0000'}, {'\u0369', '\u0000'}, {'\u036a', '\u0000'}, {'\u036b', '\u0000'},
        {'\u036c', '\u0000'}, {'\u036d', '\u0000'}, {'\u036e', '\u0000'}, {'\u036f', '\u0000'},
        {'\u0374', '\u02b9'}, {'\u037e', '\u003b'}, {'\u0385', '\u00a8'}, {'\u0386', '\u0391'},
        {'\u0387', '\u00b7'}, {'\u0388', '\u0395'}, {'\u0389', '\u0397'}, {'\u038a', '\u0399'},
        {'\u038c', '\u039f'}, {'\u038e', '\u03a5'}, {'\u038f', '\u03a9'}, {'\u0390', '\u03b9'},
        {'\u03aa', '\u0399'}, {'\u03ab', '\u03a5'}, {'\u03ac', '\u03b1'}, {'\u03ad', '\u03b5'},
        {'\u03ae', '\u03b7'}, {'\u03af', '\u03b9'}, {'\u03b0', '\u03c5'}, {'\u03ca', '\u03b9'},
        {'\u03cb', '\u03c5'}, {'\u03cc', '\u03bf'}, {'\u03cd', '\u03c5'}, {'\u03ce', '\u03c9'},
        {'\u03d3', '\u03d2'}, {'\u03d4', '\u03d2'}
        };
        #endregion

        /// <summary>
        /// Gets the segment character from the specified character.
        /// This is optimized for speed and avoids the more performance
        /// expensive call to <see cref="UniData.GetSegment(char, bool)"/>,
        /// but is limited to the Unicode range 0000-03FF.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>Sergment.</returns>
        public static char GetSegment(char c)
        {
            return _segments.ContainsKey(c)
                    ? _segments[c]
                    : c;
        }

        /// <summary>
        /// Determines whether the specified character is in the range handled
        /// by this helper.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><c>true</c> if is in range; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInRange(char c) => c <= 0x03ff;
    }
}
