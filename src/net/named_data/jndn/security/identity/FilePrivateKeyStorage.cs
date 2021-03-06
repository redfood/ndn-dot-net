// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
//
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2014-2016 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn.security.identity {
	
	using ILOG.J2CsMapping.NIO;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Security.Cryptography;
	using System.Text;
	using System.spec;
	using javax.crypto;
	using javax.crypto.spec;
	using net.named_data.jndn;
	using net.named_data.jndn.encoding.der;
	using net.named_data.jndn.security;
	using net.named_data.jndn.security.certificate;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// FilePrivateKeyStorage extends PrivateKeyStorage to implement private key
	/// storage using files.
	/// </summary>
	///
	public class FilePrivateKeyStorage : PrivateKeyStorage {
		/// <summary>
		/// Create a new FilePrivateKeyStorage to connect to the default directory in
		/// System.getProperty("user.home").
		/// </summary>
		///
		public FilePrivateKeyStorage() {
			keyStorePath_ = new FileInfo(getDefaultDirecoryPath(System.Environment.GetEnvironmentVariable("user.home")));
			System.IO.Directory.CreateDirectory(keyStorePath_.FullName);
		}
	
		/// <summary>
		/// Create a new FilePrivateKeyStorage to connect to the given directory.
		/// </summary>
		///
		/// <param name="keyStoreDirectoryPath">For example, you can get the default directory path from an Android files directory with getDefaultDirecoryPath(context.getFilesDir())</param>
		public FilePrivateKeyStorage(String keyStoreDirectoryPath) {
			keyStorePath_ = new FileInfo(keyStoreDirectoryPath);
			System.IO.Directory.CreateDirectory(keyStorePath_.FullName);
		}
	
		/// <summary>
		/// Get the default directory path for private keys based on the files root.
		/// For example if filesRoot is "/data/data/org.example/files", this returns
		/// "/data/data/org.example/files/.ndn/ndnsec-tpm-file".
		/// </summary>
		///
		/// <param name="filesRoot"></param>
		/// <returns>The default directory path.</returns>
		public static String getDefaultDirecoryPath(FileInfo filesRoot) {
			return getDefaultDirecoryPath(filesRoot.FullName);
		}
	
		/// <summary>
		/// Get the default directory path for private keys based on the files root.
		/// </summary>
		///
		/// <param name="filesRoot">The root file directory.</param>
		/// <returns>The default directory path.</returns>
		public static String getDefaultDirecoryPath(String filesRoot) {
			// NOTE: Use File because java.nio.file.Path is not available before Java 7.
			return new FileInfo(System.IO.Path.Combine(new FileInfo(filesRoot+".ndn").FullName,"ndnsec-tpm-file")).FullName;
		}
	
		/// <summary>
		/// Generate a pair of asymmetric keys.
		/// </summary>
		///
		/// <param name="keyName">The name of the key pair.</param>
		/// <param name="params">The parameters of the key.</param>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override void generateKeyPair(Name keyName, KeyParams paras) {
			if (doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.PUBLIC))
				throw new SecurityException("Public Key already exists");
			if (doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.PRIVATE))
				throw new SecurityException("Private Key already exists");
	
			String keyAlgorithm;
			int keySize;
			if (paras.getKeyType() == net.named_data.jndn.security.KeyType.RSA) {
				keyAlgorithm = "RSA";
				keySize = ((RsaKeyParams) paras).getKeySize();
			} else if (paras.getKeyType() == net.named_data.jndn.security.KeyType.ECDSA) {
				keyAlgorithm = "EC";
				keySize = ((EcdsaKeyParams) paras).getKeySize();
			} else
				throw new SecurityException("Cannot generate a key pair of type "
						+ paras.getKeyType());
	
			KeyPairGenerator generator = null;
			try {
				generator = System.KeyPairGenerator.getInstance(keyAlgorithm);
			} catch (Exception e) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Could not create the key generator: "
								+ e.Message);
			}
	
			// generate
			generator.initialize(keySize);
			KeyPair pair = generator.generateKeyPair();
	
			// save
			this.write(keyName, net.named_data.jndn.security.KeyClass.PRIVATE, pair.getPrivate().getEncoded());
			this.write(keyName, net.named_data.jndn.security.KeyClass.PUBLIC, pair.getPublic().getEncoded());
		}
	
		/// <summary>
		/// Delete a pair of asymmetric keys. If the key doesn't exist, do nothing.
		/// </summary>
		///
		/// <param name="keyName">The name of the key pair.</param>
		public override void deleteKeyPair(Name keyName) {
			try {
				// deleteKeyPair is required by an older API which will be changed.
				// For now, call deleteKey.
				deleteKey(keyName);
			} catch (SecurityException ex) {
				// In the deleteKeyPair API, do nothing if the key doesn't exist.
			}
		}
	
		/// <summary>
		/// Get the public key
		/// </summary>
		///
		/// <param name="keyName">The name of public key.</param>
		/// <returns>The public key.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override PublicKey getPublicKey(Name keyName) {
			if (!doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.PUBLIC))
				throw new SecurityException("Public key does not exist.");
	
			// Read the file contents.
			byte[] der = this.read(keyName, net.named_data.jndn.security.KeyClass.PUBLIC);
	
			return new PublicKey(new Blob(der, false));
		}
	
		/// <summary>
		/// Get the private key for this name; internal helper method
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <param name="keyType">Set keyType[0] to the KeyType.</param>
		/// <returns>The java.security.PrivateKey.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		private PrivateKey getPrivateKey(Name keyName, KeyType[] keyType) {
			if (!doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.PRIVATE))
				throw new SecurityException(
						"FilePrivateKeyStorage: Private key does not exist.");
	
			// Read the file contents.
			byte[] der = this.read(keyName, net.named_data.jndn.security.KeyClass.PRIVATE);
	
			// Decode the PKCS #8 DER to find the algorithm OID.
			String oidString = null;
			try {
				DerNode parsedNode = net.named_data.jndn.encoding.der.DerNode.parse(ILOG.J2CsMapping.NIO.ByteBuffer.wrap(der), 0);
				IList pkcs8Children = parsedNode.getChildren();
				IList algorithmIdChildren = net.named_data.jndn.encoding.der.DerNode.getSequence(pkcs8Children, 1)
						.getChildren();
				oidString = ((DerNode.DerOid) algorithmIdChildren[0]).toVal()
						.toString();
			} catch (DerDecodingException ex) {
				throw new SecurityException(
						"Cannot decode the PKCS #8 private key: " + ex);
			}
	
			PKCS8EncodedKeySpec spec = new PKCS8EncodedKeySpec(der);
			if (oidString.equals(RSA_ENCRYPTION_OID)) {
				keyType[0] = net.named_data.jndn.security.KeyType.RSA;
	
				try {
					KeyFactory kf = System.KeyFactory.getInstance("RSA");
					return kf.generatePrivate(spec);
				} catch (InvalidKeySpecException e) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: RSA is not supported: "
									+ e.Message);
				} catch (Exception e_0) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: PKCS8EncodedKeySpec is not supported for RSA: "
									+ e_0.Message);
				}
			} else if (oidString.equals(EC_ENCRYPTION_OID)) {
				keyType[0] = net.named_data.jndn.security.KeyType.ECDSA;
	
				try {
					KeyFactory kf_1 = System.KeyFactory.getInstance("EC");
					return kf_1.generatePrivate(spec);
				} catch (InvalidKeySpecException e_2) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: EC is not supported: "
									+ e_2.Message);
				} catch (Exception e_3) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: PKCS8EncodedKeySpec is not supported for EC: "
									+ e_3.Message);
				}
			} else
				throw new SecurityException(
						"FilePrivateKeyStorage::sign: Unrecognized private key OID: "
								+ oidString);
		}
	
		/// <summary>
		/// Get the symmetric key for this name; internal helper method
		/// </summary>
		///
		/// <param name="keyName"></param>
		/// <returns>The symmetric key.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		private SecretKey getSymmetricKey(Name keyName) {
			if (!doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.SYMMETRIC))
				throw new SecurityException(
						"FilePrivateKeyStorage: Symmetric key does not exist.");
	
			// Read the file contents.
			byte[] encoded = this.read(keyName, net.named_data.jndn.security.KeyClass.SYMMETRIC);
			// TODO: Check the key type. Don't assume AES.
			return new SecretKeySpec(encoded, "AES");
		}
	
		/// <summary>
		/// Fetch the private key for keyName and sign the data, returning a signature
		/// Blob.
		/// </summary>
		///
		/// <param name="data">Pointer the input byte buffer to sign.</param>
		/// <param name="keyName">The name of the signing key.</param>
		/// <param name="digestAlgorithm">the digest algorithm.</param>
		/// <returns>The signature Blob.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override Blob sign(ByteBuffer data, Name keyName,
				DigestAlgorithm digestAlgorithm) {
			if (!doesKeyExist(keyName, net.named_data.jndn.security.KeyClass.PRIVATE))
				throw new SecurityException(
						"FilePrivateKeyStorage.sign: private key doesn't exist");
	
			if (digestAlgorithm != net.named_data.jndn.security.DigestAlgorithm.SHA256)
				throw new SecurityException(
						"FilePrivateKeyStorage.sign: Unsupported digest algorithm");
	
			// Retrieve the private key.
			KeyType[] keyType = new KeyType[1];
			PrivateKey privateKey = getPrivateKey(keyName, keyType);
	
			// Sign.
			System.SecuritySignature signature = null;
			if (keyType[0] == net.named_data.jndn.security.KeyType.RSA) {
				try {
					signature = System.SecuritySignature
							.getInstance("SHA256withRSA");
				} catch (Exception e) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: The SHA256withRSA algorithm is not supported");
				}
			} else if (keyType[0] == net.named_data.jndn.security.KeyType.ECDSA) {
				try {
					signature = System.SecuritySignature
							.getInstance("SHA256withECDSA");
				} catch (Exception e_0) {
					// Don't expect this to happen.
					throw new SecurityException(
							"FilePrivateKeyStorage: The SHA256withECDSA algorithm is not supported");
				}
			} else
				// We don't expect this to happen since getPrivateKey checked it.
				throw new SecurityException(
						"FilePrivateKeyStorage: Unsupported signature key type "
								+ keyType[0]);
	
			try {
				signature.initSign(privateKey);
			} catch (InvalidKeyException exception) {
				throw new SecurityException(
						"FilePrivateKeyStorage: InvalidKeyException: "
								+ exception.Message);
			}
			try {
				signature.update(data);
				return new Blob(signature.sign(), false);
			} catch (SignatureException exception_1) {
				throw new SecurityException(
						"FilePrivateKeyStorage: SignatureException: "
								+ exception_1.Message);
			}
		}
	
		/// <summary>
		/// Decrypt data.
		/// </summary>
		///
		/// <param name="keyName">The name of the decrypting key.</param>
		/// <param name="data"></param>
		/// <param name="isSymmetric"></param>
		/// <returns>The decrypted data.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override Blob decrypt(Name keyName, ByteBuffer data, bool isSymmetric) {
			throw new NotSupportedException(
					"FilePrivateKeyStorage.decrypt is not implemented");
		}
	
		/// <summary>
		/// Encrypt data.
		/// </summary>
		///
		/// <param name="keyName">The name of the encrypting key.</param>
		/// <param name="data"></param>
		/// <param name="isSymmetric"></param>
		/// <returns>The encrypted data.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override Blob encrypt(Name keyName, ByteBuffer data, bool isSymmetric) {
			throw new NotSupportedException(
					"FilePrivateKeyStorage.encrypt is not implemented");
		}
	
		/// <summary>
		/// Generate a symmetric key.
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <param name="params">The parameters of the key.</param>
		/// <exception cref="System.Security.SecurityException"></exception>
		public sealed override void generateKey(Name keyName, KeyParams paras) {
			throw new NotSupportedException(
					"FilePrivateKeyStorage.generateKey is not implemented");
		}
	
		/// <summary>
		/// Delete a key by name; checks all KeyClass types
		/// </summary>
		///
		/// <param name="keyName"></param>
		/// <exception cref="System.Security.SecurityException"></exception>
		public void deleteKey(Name keyName) {
			int deletedFiles = 0;
			/* foreach */
			foreach (KeyClass keyClass  in  (KeyClass[]) Enum.GetValues(typeof(KeyClass))) {
				if (doesKeyExist(keyName, keyClass)) {
					String extension = (String) ILOG.J2CsMapping.Collections.Collections.Get(keyTypeMap_,keyClass);
					FileInfo file = nameTransform(keyName.toUri(), extension);
					file.delete();
					deletedFiles++;
				}
			}
			if (deletedFiles == 0) {
				throw new SecurityException("No key files found to delete");
			}
		}
	
		/// <summary>
		/// Check if a particular key exists.
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <param name="keyClass"></param>
		/// <returns>True if the key exists, otherwise false.</returns>
		public sealed override bool doesKeyExist(Name keyName, KeyClass keyClass) {
			String keyURI = keyName.toUri();
			String extension = (String) ILOG.J2CsMapping.Collections.Collections.Get(keyTypeMap_,keyClass);
			if (extension == null)
				throw new SecurityException("Unrecognized key class");
			else
				return nameTransform(keyURI, extension).Exists;
		}
	
		/// <summary>
		/// Transform a key name to its hashed file path
		/// </summary>
		///
		/// <param name="keyName"></param>
		/// <param name="extension"></param>
		/// <returns>The hashed file path.</returns>
		/// <exception cref="System.Security.SecurityException"></exception>
		private FileInfo nameTransform(String keyName, String extension) {
			MD5 sha256;
			try {
				sha256 = System.Security.Cryptography.MD5.Create();
			} catch (Exception exception) {
				// Don't expect this to happen.
				throw new Exception("MessageDigest: SHA-256 is not supported: "
						+ exception.Message);
			}
			sha256.ComputeHash(ILOG.J2CsMapping.Util.StringUtil.GetBytes(keyName));
			byte[] hash = sha256.Hash;
	
			String digest = net.named_data.jndn.util.Common.base64Encode(hash);
			digest = digest.replace('/', '%');
	
			return new FileInfo(System.IO.Path.Combine(keyStorePath_.FullName,digest + extension));
		}
	
		/// <summary>
		/// Use nameTransform to get the file path for keyName (without the extension)
		/// and also add to the mapping.txt file.
		/// </summary>
		///
		/// <param name="keyName">The key name which is transformed to a file path.</param>
		/// <returns>The key file path without the extension.</returns>
		private String maintainMapping(String keyName) {
			String keyFilePathNoExtension = System.IO.Path.GetFullPath(nameTransform(keyName, "").Name);
	
			FileInfo mappingFilePath = new FileInfo(System.IO.Path.Combine(keyStorePath_.FullName,"mapping.txt"));
	
			try {
				BufferedStream writer = new BufferedStream(new StreamWriter(
						mappingFilePath, true));
				try {
					writer.write(keyName + ' ' + keyFilePathNoExtension + '\n');
					writer.flush();
				} finally {
					writer.close();
				}
			} catch (IOException e) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Failed to write to mapping.txt: "
								+ e.Message);
			}
	
			return keyFilePathNoExtension;
		}
	
		/// <summary>
		/// Write to a key file. If keyClass is PRIVATE, then also update mapping.txt.
		/// </summary>
		///
		/// <param name="keyName"></param>
		/// <param name="keyClass">[PUBLIC, PRIVATE, SYMMETRIC]</param>
		/// <param name="data"></param>
		/// <exception cref="IOException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		private void write(Name keyName, KeyClass keyClass, byte[] data) {
			String extension = (String) ILOG.J2CsMapping.Collections.Collections.Get(keyTypeMap_,keyClass);
			try {
				String filePath;
				if (keyClass == net.named_data.jndn.security.KeyClass.PRIVATE)
					filePath = maintainMapping(keyName.toUri()) + extension;
				else
					filePath = System.IO.Path.GetFullPath(nameTransform(keyName.toUri(), extension).Name);
	
				BufferedStream writer = new BufferedStream(new StreamWriter(filePath));
				try {
					String base64Data = net.named_data.jndn.util.Common.base64Encode(data);
					writer.Write(base64Data,0,base64Data.Substring(0,base64Data.Length));
					writer.flush();
				} finally {
					writer.close();
				}
			} catch (SecurityException e) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Failed to write key: "
								+ e.Message);
			} catch (IOException e_0) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Failed to write key: "
								+ e_0.Message);
			}
		}
	
		/// <summary>
		/// Read from a key file
		/// </summary>
		///
		/// <param name="keyName"></param>
		/// <param name="keyClass">[PUBLIC, PRIVATE, SYMMETRIC]</param>
		/// <returns>The key bytes.</returns>
		/// <exception cref="IOException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		private byte[] read(Name keyName, KeyClass keyClass) {
			String extension = (String) ILOG.J2CsMapping.Collections.Collections.Get(keyTypeMap_,keyClass);
			StringBuilder contents = new StringBuilder();
			try {
				TextReader reader = new System.IO.StreamReader(nameTransform(keyName.toUri(), extension).OpenWrite());
				// Use "try/finally instead of "try-with-resources" or "using"
				// which are not supported before Java 7.
				try {
					String line = null;
					while ((line = reader.readLine()) != null)
						contents.append(line);
				} finally {
					reader.close();
				}
			} catch (SecurityException e) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Failed to read key: "
								+ e.Message);
			} catch (IOException e_0) {
				throw new SecurityException(
						"FilePrivateKeyStorage: Failed to read key: "
								+ e_0.Message);
			}
	
			return net.named_data.jndn.util.Common.base64Decode(contents.toString());
		}
	
		static private String RSA_ENCRYPTION_OID = "1.2.840.113549.1.1.1";
		static private String EC_ENCRYPTION_OID = "1.2.840.10045.2.1";
	
		private readonly FileInfo keyStorePath_;
		// Use HashMap without generics so it works with older Java compilers.
		private static readonly Hashtable keyTypeMap_;
		static FilePrivateKeyStorage() {
				keyTypeMap_ = new Hashtable();
				ILOG.J2CsMapping.Collections.Collections.Put(keyTypeMap_,net.named_data.jndn.security.KeyClass.PUBLIC,".pub");
				ILOG.J2CsMapping.Collections.Collections.Put(keyTypeMap_,net.named_data.jndn.security.KeyClass.PRIVATE,".pri");
				ILOG.J2CsMapping.Collections.Collections.Put(keyTypeMap_,net.named_data.jndn.security.KeyClass.SYMMETRIC,".key");
			}
	}
}
