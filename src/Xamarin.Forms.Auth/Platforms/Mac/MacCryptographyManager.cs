﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Xamarin.Forms.Auth
{
    internal class MacCryptographyManager : ICryptographyManager
    {
        public string CreateBase64UrlEncodedSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            using (SHA256Managed sha = new SHA256Managed())
            {
                UTF8Encoding encoding = new UTF8Encoding();
                return Base64UrlHelpers.Encode(sha.ComputeHash(encoding.GetBytes(input)));
            }
        }

        public string GenerateCodeVerifier()
        {
            byte[] buffer = new byte[Constants.CodeVerifierByteSize];
            using (RNGCryptoServiceProvider randomSource = new RNGCryptoServiceProvider())
            {
                randomSource.GetBytes(buffer);
            }

            return Base64UrlHelpers.Encode(buffer);
        }

        public string CreateSha256Hash(string input)
        {
            return string.IsNullOrEmpty(input) ? null : Convert.ToBase64String(CreateSha256HashBytes(input));
        }

        public byte[] CreateSha256HashBytes(string input)
        {
            using (var sha = new SHA256Managed())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        public string Encrypt(string message)
        {
            throw new NotImplementedException();
        }

        public string Decrypt(string encryptedMessage)
        {
            throw new NotImplementedException();
        }

        public byte[] Encrypt(byte[] message)
        {
            throw new NotImplementedException();
        }

        public byte[] Decrypt(byte[] encryptedMessage)
        {
            throw new NotImplementedException();
        }

        public byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            throw new NotImplementedException();
        }
    }
}
