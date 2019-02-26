using System;
using System.IO;
using System.Text;
using Menu.Logger;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;

namespace Menu.Functional.Pass
{
    public class Validator
    {
		private const int MIN_LENGTH = 6;
        private const int MAX_LENGTH = 20;
        private EmailAddressAttribute foo = new EmailAddressAttribute();
		private LogProgram logger;
        private Random random = new Random();
        private string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private string sum_names = "abc123";

        public Validator(LogProgram logger) 
		{
			this.logger = logger;
        }
        public bool ValidateEmail(string email)
        {
            bool bar = foo.IsValid(email);
            logger.WriteLog("Validating email=" + email + " success=" + bar, LogLevel.Validator);
            return bar;
        } 
		public bool ValidatePassword(string password, ref string answer) 
		{
			if (string.IsNullOrEmpty(password)) 
            { 
                return false; 
            }
			bool meetsLengthRequirements = password.Length >= MIN_LENGTH && password.Length <= MAX_LENGTH;
			bool hasUpperCaseLetter = false;
			bool hasLowerCaseLetter = false;
			bool hasDecimalDigit = false;
			if (meetsLengthRequirements) 
			{
				foreach (char c in password)
				{
					if (char.IsUpper(c)) hasUpperCaseLetter = true;
					else if (char.IsLower(c)) hasLowerCaseLetter = true;
					else if (char.IsDigit(c)) hasDecimalDigit = true;
				}
			}
            if (hasUpperCaseLetter == false)
            {
                answer = "Current password does not has upper case letter.";
            }
            if (hasLowerCaseLetter == false)
            {
                answer = "Current password does not has lower case letter.";
            }
            if (hasDecimalDigit == false)
            {
                answer = "Current password does not has decimal digit.";
            }
            bool isValid = meetsLengthRequirements && hasUpperCaseLetter && hasLowerCaseLetter 	&& hasDecimalDigit;
			logger.WriteLog("Validating password success=" + isValid + ".", LogLevel.Validator);
			return isValid;         
        }
		public bool EqualsPasswords(string password, string confirmpassword) 
		{
            bool answer = password.Equals(confirmpassword);
			logger.WriteLog("Validating confirm password=" + answer + ".", LogLevel.Validator);
			return answer;
		}
        public string GenerateHash(int length_hash)
        {
            string hash = "";
            for (int i = 0; i < length_hash; i++)
            {
                hash += Alphavite[random.Next(Alphavite.Length)];
            }
            return hash;
        }
        public string HashPassword(string password)
        {
            byte[] salt;
            byte[] buffer2;
            if (password == null)
            {
                return "";
            }
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
        public bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                return false;
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return ByteArraysEqual(buffer3, buffer4);
        }
        private static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2)
            {
                return true;
            }
            if (b1 == null || b2 == null)
            { 
                return false; 
            }
            if (b1.Length != b2.Length)
            {
                return false;
            }
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
        public string Encrypt(string clearText)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sum_names,  new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public string Decrypt(string cipherText)
        {
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(sum_names, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}
