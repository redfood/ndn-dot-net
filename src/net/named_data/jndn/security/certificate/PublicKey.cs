// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
//
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2013-2016 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn.security.certificate {
	
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.spec;
	using net.named_data.jndn.encoding.der;
	using net.named_data.jndn.security;
	using net.named_data.jndn.util;
	
	public class PublicKey {
		public PublicKey() {
			keyType_ =  default(KeyType)/* was: null */;
			keyDer_ = new Blob();
		}
	
		/// <summary>
		/// Create a new PublicKey by decoding the keyDer. Set the key type from the
		/// decoding.
		/// </summary>
		///
		/// <param name="keyDer">The blob of the SubjectPublicKeyInfo DER.</param>
		/// <exception cref="UnrecognizedKeyFormatException">if can't decode the key DER.</exception>
		public PublicKey(Blob keyDer) {
			keyDer_ = keyDer;
	
			// Get the public key OID.
			String oidString = null;
			try {
				DerNode parsedNode = net.named_data.jndn.encoding.der.DerNode.parse(keyDer.buf(), 0);
				IList rootChildren = parsedNode.getChildren();
				IList algorithmIdChildren = net.named_data.jndn.encoding.der.DerNode.getSequence(rootChildren, 0)
						.getChildren();
				oidString = "" + ((DerNode) algorithmIdChildren[0]).toVal();
			} catch (DerDecodingException ex) {
				throw new UnrecognizedKeyFormatException(
						"PublicKey: Error decoding the public key: "
								+ ex.Message);
			}
	
			// Verify that the we can decode.
			if (oidString.equals(RSA_ENCRYPTION_OID)) {
				keyType_ = net.named_data.jndn.security.KeyType.RSA;
	
				KeyFactory keyFactory = null;
				try {
					keyFactory = System.KeyFactory.getInstance("RSA");
				} catch (Exception exception) {
					// Don't expect this to happen.
					throw new UnrecognizedKeyFormatException(
							"RSA is not supported: " + exception.Message);
				}
	
				try {
					keyFactory.generatePublic(new X509EncodedKeySpec(keyDer
							.getImmutableArray()));
				} catch (InvalidKeySpecException exception_0) {
					// Don't expect this to happen.
					throw new UnrecognizedKeyFormatException(
							"X509EncodedKeySpec is not supported for RSA: "
									+ exception_0.Message);
				}
			} else if (oidString.equals(EC_ENCRYPTION_OID)) {
				keyType_ = net.named_data.jndn.security.KeyType.ECDSA;
	
				KeyFactory keyFactory_1 = null;
				try {
					keyFactory_1 = System.KeyFactory.getInstance("EC");
				} catch (Exception exception_2) {
					// Don't expect this to happen.
					throw new UnrecognizedKeyFormatException(
							"EC is not supported: " + exception_2.Message);
				}
	
				try {
					keyFactory_1.generatePublic(new X509EncodedKeySpec(keyDer
							.getImmutableArray()));
				} catch (InvalidKeySpecException exception_3) {
					// Don't expect this to happen.
					throw new UnrecognizedKeyFormatException(
							"X509EncodedKeySpec is not supported for EC: "
									+ exception_3.Message);
				}
			} else
				throw new UnrecognizedKeyFormatException(
						"PublicKey: Unrecognized OID " + oidString);
		}
	
		/// <summary>
		/// Encode the public key into DER.
		/// </summary>
		///
		/// <returns>the encoded DER syntax tree.</returns>
		public DerNode toDer() {
			return net.named_data.jndn.encoding.der.DerNode.parse(keyDer_.buf());
		}
	
		public KeyType getKeyType() {
			return keyType_;
		}
	
		/*
		 * Get the digest of the public key.
		 * @param digestAlgorithm The digest algorithm.
		 */
		public Blob getDigest(DigestAlgorithm digestAlgorithm) {
			if (digestAlgorithm == net.named_data.jndn.security.DigestAlgorithm.SHA256) {
				return new Blob(net.named_data.jndn.util.Common.digestSha256(keyDer_.buf()), false);
			} else
				throw new UnrecognizedDigestAlgorithmException("Wrong format!");
		}
	
		/*
		 * Get the digest of the public key using DigestAlgorithm.SHA256.
		 */
		public Blob getDigest() {
			try {
				return getDigest(net.named_data.jndn.security.DigestAlgorithm.SHA256);
			} catch (UnrecognizedDigestAlgorithmException ex) {
				// We don't expect this exception.
				throw new Exception("UnrecognizedDigestAlgorithmException "
						+ ex.Message);
			}
		}
	
		/*
		 * Get the raw bytes of the public key in DER format.
		 */
		public Blob getKeyDer() {
			return keyDer_;
		}
	
		private static String RSA_ENCRYPTION_OID = "1.2.840.113549.1.1.1";
		private static String EC_ENCRYPTION_OID = "1.2.840.10045.2.1";
	
		private readonly KeyType keyType_;
		private readonly Blob keyDer_;
		/**< PublicKeyInfo in DER */
	}
}
