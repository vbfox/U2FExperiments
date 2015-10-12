// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Server.data
{
	public class SecurityKeyData
	{
		public enum Transports
		{
			BluetoothRadio,
			BluetoothLowEnergy,
			Usb,
			Nfc
		}

		private readonly long enrollmentTime;

		private readonly System.Collections.Generic.IList<Transports
			> transports;

		private readonly byte[] keyHandle;

		private readonly byte[] publicKey;

		private readonly rg.BouncyCastle.X509.X509Certificate attestationCert;

		private int counter;

		public SecurityKeyData(long enrollmentTime, byte[] keyHandle, byte[] publicKey, rg.BouncyCastle.X509.X509Certificate
			 attestationCert, int counter)
			: this(enrollmentTime, null, keyHandle, publicKey, attestationCert, counter)
		{
		}

		public SecurityKeyData(long enrollmentTime, System.Collections.Generic.IList<Transports
			> transports, byte[] keyHandle, byte[] publicKey, rg.BouncyCastle.X509.X509Certificate
			 attestationCert, int counter)
		{
			/* transports */
			this.enrollmentTime = enrollmentTime;
			this.transports = transports;
			this.keyHandle = keyHandle;
			this.publicKey = publicKey;
			this.attestationCert = attestationCert;
			this.counter = counter;
		}

		/// <summary>When these keys were created/enrolled with the relying party.</summary>
		public virtual long GetEnrollmentTime()
		{
			return enrollmentTime;
		}

		public virtual System.Collections.Generic.IList<Transports
			> GetTransports()
		{
			return transports;
		}

		public virtual byte[] GetKeyHandle()
		{
			return keyHandle;
		}

		public virtual byte[] GetPublicKey()
		{
			return publicKey;
		}

		public virtual rg.BouncyCastle.X509.X509Certificate GetAttestationCertificate()
		{
			return attestationCert;
		}

		public virtual int GetCounter()
		{
			return counter;
		}

		public virtual void SetCounter(int newCounterValue)
		{
			counter = newCounterValue;
		}

		public override int GetHashCode()
		{
			return com.google.common.@base.Objects.hashCode(enrollmentTime, transports, keyHandle
				, publicKey, attestationCert);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SecurityKeyData))
			{
				return false;
			}
			SecurityKeyData that = (SecurityKeyData
				)obj;
			return java.util.Arrays.equals(keyHandle, that.keyHandle) && (enrollmentTime
				 == that.enrollmentTime) && ContainSameTransports(transports, that.transports
				) && java.util.Arrays.equals(publicKey, that.publicKey) && com.google.common.@base.Objects
				.equal(attestationCert, that.attestationCert);
		}

		/// <summary>Compares the two Lists of Transports and says if they are equal.</summary>
		/// <param name="transports1">first List of Transports</param>
		/// <param name="transports2">second List of Transports</param>
		/// <returns>true if both lists are null or if both lists contain the same transport values
		/// 	</returns>
		public static bool ContainSameTransports(System.Collections.Generic.IList<Transports
			> transports1, System.Collections.Generic.IList<Transports
			> transports2)
		{
			if (transports1 == null && transports2 == null)
			{
				return true;
			}
			else
			{
				if (transports1 == null || transports2 == null)
				{
					return false;
				}
			}
			return transports1.containsAll(transports2) && transports2.containsAll(transports1
				);
		}

		public override string ToString()
		{
			return new java.lang.StringBuilder().Append("public_key: ").Append(org.apache.commons.codec.binary.Base64
				.encodeBase64URLSafeString(publicKey)).Append("\n").Append("key_handle: ").Append
				(WebSafeBase64Converter.ToBase64String(keyHandle)).Append
				("\n").Append("counter: ").Append(counter).Append("\n").Append("attestation certificate:\n"
				).Append(attestationCert.ToString()).Append("transports: ").Append(transports).Append
				("\n").ToString();
		}
	}
}
