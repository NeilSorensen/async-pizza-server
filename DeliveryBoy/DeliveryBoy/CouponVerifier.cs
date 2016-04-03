using System;
using System.Security.Cryptography;
using System.Text;

namespace DeliveryBoy
{
    public class CouponVerifier
    {
        private const string CouponSecret = "This is a very secret coupon code";

        private readonly HashAlgorithm hashAlgorithm;

        public CouponVerifier()
        {
            hashAlgorithm = new SHA1CryptoServiceProvider();
        }

        public string GenerateCoupon(string customer)
        {
            var bytes = Encoding.UTF8.GetBytes(customer + CouponSecret);
            var hashBytes = hashAlgorithm.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public bool CouponIsValid(string coupon, string customer)
        {
            return coupon == GenerateCoupon(customer);
        }
    }
}