using Xunit;
using System;

namespace FileDuplicateFinder.Utilities.Tests {

    public class CommonUtilitiesTests {

        #region PrettyPrintSize tests
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1, "1 B")]
        [InlineData(25, "25 B")]
        [InlineData(512, "512 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1,00 KiB")]
        [InlineData(1029, "1,00 KiB")]
        [InlineData(1030, "1,01 KiB")]
        [InlineData(1300, "1,27 KiB")]
        [InlineData(1000000, "976 KiB")]
        [InlineData(1048576, "1,00 MiB")]
        [InlineData(129452999, "123 MiB")]
        [InlineData(1325607178, "1,23 GiB")]
        [InlineData(21474836480, "20,0 GiB")]
        [InlineData(22441204122, "20,9 GiB")]
        [InlineData(549755813888000, "500 TiB")]
        [InlineData(1125899906842624000, "1000 PiB")]
        [InlineData(long.MaxValue, "8,00 EiB")]
        public void PrettyPrintSize_ShouldReturnCorrectStrings(long bytes, string expectedResult) {
            string result = CommonUtilities.PrettyPrintSize(bytes);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void PrettyPrintSize_ShouldThrowArgumentException_ForNegativeBytes() {
            const long bytes = -123;

            Assert.Throws<ArgumentException>(() => CommonUtilities.PrettyPrintSize(bytes));
        }
        #endregion
    }
}
