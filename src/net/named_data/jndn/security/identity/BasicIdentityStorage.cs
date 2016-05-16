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
	
	using ILOG.J2CsMapping.Util.Logging;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Data.SqlClient;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn;
	using net.named_data.jndn.encoding;
	using net.named_data.jndn.security;
	using net.named_data.jndn.security.certificate;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// BasicIdentityStorage extends IdentityStorage to implement basic storage of
	/// identity, public keys and certificates using the org.sqlite.JDBC SQLite
	/// provider.
	/// </summary>
	///
	public class BasicIdentityStorage : Sqlite3IdentityStorageBase {
		/// <summary>
		/// Create a new BasicIdentityStorage to use the SQLite3 file in the
		/// default location.
		/// </summary>
		///
		public BasicIdentityStorage() {
			this.database_ = null;
			// NOTE: Use File because java.nio.file.Path is not available before Java 7.
			FileInfo identityDir = new FileInfo(System.Environment.GetEnvironmentVariable("user.home")+".ndn");
			System.IO.Directory.CreateDirectory(identityDir.FullName);
			FileInfo databasePath = new FileInfo(System.IO.Path.Combine(identityDir.FullName,"ndnsec-public-info.db"));
			construct(databasePath.FullName);
		}
	
		/// <summary>
		/// Create a new BasicIdentityStorage to use the given SQLite3 file.
		/// </summary>
		///
		/// <param name="databaseFilePath">The path of the SQLite file.</param>
		public BasicIdentityStorage(String databaseFilePath) {
			this.database_ = null;
			construct(databaseFilePath);
		}
	
		private void construct(String databaseFilePath) {
			try {
				ILOG.J2CsMapping.Reflect.Helper.GetNativeType("org.sqlite.JDBC");
			} catch (TypeLoadException ex) {
				// We don't expect this to happen.
				ILOG.J2CsMapping.Util.Logging.Logger.getLogger(typeof(BasicIdentityStorage).FullName).log(
						ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
				return;
			}
	
			try {
				database_ = System.Data.SqlClient.DriverManager.getConnection("jdbc:sqlite:"
						+ databaseFilePath);
	
				SqlCommand statement = database_.CreateCommand();
				// Use "try/finally instead of "try-with-resources" or "using" which are not supported before Java 7.
				try {
					// Check if the TpmInfo table exists.
					SqlDataReader result = statement
							.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_MASTER_TPM_INFO_TABLE);
					bool tpmInfoTableExists = false;
					if (result.NextResult())
						tpmInfoTableExists = true;
					result.close();
	
					if (!tpmInfoTableExists)
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_TPM_INFO_TABLE);
	
					// Check if the ID table exists.
					result = statement.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_MASTER_ID_TABLE);
					bool idTableExists = false;
					if (result.NextResult())
						idTableExists = true;
					result.close();
	
					if (!idTableExists) {
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_ID_TABLE1);
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_ID_TABLE2);
					}
	
					// Check if the Key table exists.
					result = statement.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_MASTER_KEY_TABLE);
					idTableExists = false;
					if (result.NextResult())
						idTableExists = true;
					result.close();
	
					if (!idTableExists) {
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_KEY_TABLE1);
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_KEY_TABLE2);
					}
	
					// Check if the Certificate table exists.
					result = statement.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_MASTER_CERT_TABLE);
					idTableExists = false;
					if (result.NextResult())
						idTableExists = true;
					result.close();
	
					if (!idTableExists) {
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_CERT_TABLE1);
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_CERT_TABLE2);
						statement.executeUpdate(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.INIT_CERT_TABLE3);
					}
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Check if the specified identity already exists.
		/// </summary>
		///
		/// <param name="identityName">The identity name.</param>
		/// <returns>True if the identity exists, otherwise false.</returns>
		public sealed override bool doesIdentityExist(Name identityName) {
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_doesIdentityExist);
				statement.setString(1, identityName.toUri());
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return result.getInt(1) > 0;
					else
						return false;
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Add a new identity. Do nothing if the identity already exists.
		/// </summary>
		///
		/// <param name="identityName">The identity name to be added.</param>
		public sealed override void addIdentity(Name identityName) {
			if (doesIdentityExist(identityName))
				return;
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("INSERT INTO Identity (identity_name) values (?)");
				statement.setString(1, identityName.toUri());
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Revoke the identity.
		/// </summary>
		///
		/// <returns>True if the identity was revoked, false if not.</returns>
		public sealed override bool revokeIdentity() {
			//TODO:
			return false;
		}
	
		/// <summary>
		/// Check if the specified key already exists.
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <returns>true if the key exists, otherwise false.</returns>
		public sealed override bool doesKeyExist(Name keyName) {
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_doesKeyExist);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return result.getInt(1) > 0;
					else
						return false;
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Add a public key to the identity storage. Also call addIdentity to ensure
		/// that the identityName for the key exists. However, if the key already
		/// exists, do nothing.
		/// </summary>
		///
		/// <param name="keyName">The name of the public key to be added.</param>
		/// <param name="keyType">Type of the public key to be added.</param>
		/// <param name="publicKeyDer">A blob of the public key DER to be added.</param>
		public sealed override void addKey(Name keyName, KeyType keyType, Blob publicKeyDer) {
			if (keyName.size() == 0)
				return;
	
			if (doesKeyExist(keyName))
				return;
	
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			addIdentity(identityName);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("INSERT INTO Key (identity_name, key_identifier, key_type, public_key) values (?, ?, ?, ?)");
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
				statement.setInt(3, keyType.getNumericType());
				statement.setBytes(4, publicKeyDer.getImmutableArray());
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Get the public key DER blob from the identity storage.
		/// </summary>
		///
		/// <param name="keyName">The name of the requested public key.</param>
		/// <returns>The DER Blob.</returns>
		/// <exception cref="System.Security.SecurityException">if the key doesn't exist.</exception>
		public sealed override Blob getKey(Name keyName) {
			if (keyName.size() == 0)
				throw new SecurityException(
						"BasicIdentityStorage::getKey: Empty keyName");
	
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getKey);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return new Blob(result.getBytes("public_key"), false);
					else
						throw new SecurityException(
								"BasicIdentityStorage::getKey: The key does not exist");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// In table Key, set 'active' to isActive for the keyName.
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <param name="isActive">The value for the 'active' field.</param>
		protected internal override void updateKeyStatus(Name keyName, bool isActive) {
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("UPDATE Key SET active=? WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_updateKeyStatus);
				statement.setInt(1, ((isActive) ? 1 : 0));
				statement.setString(2, identityName.toUri());
				statement.setString(3, keyId);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Check if the specified certificate already exists.
		/// </summary>
		///
		/// <param name="certificateName">The name of the certificate.</param>
		/// <returns>True if the certificate exists, otherwise false.</returns>
		public sealed override bool doesCertificateExist(Name certificateName) {
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_doesCertificateExist);
				statement.setString(1, certificateName.toUri());
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return result.getInt(1) > 0;
					else
						return false;
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Add a certificate to the identity storage. Also call addKey to ensure that
		/// the certificate key exists. If the certificate is already installed, don't
		/// replace it.
		/// </summary>
		///
		/// <param name="certificate"></param>
		public sealed override void addCertificate(IdentityCertificate certificate) {
			Name certificateName = certificate.getName();
			Name keyName = certificate.getPublicKeyName();
	
			addKey(keyName, certificate.getPublicKeyInfo().getKeyType(),
					certificate.getPublicKeyInfo().getKeyDer());
	
			if (doesCertificateExist(certificateName))
				return;
	
			// Insert the certificate.
			try {
				PreparedStatement statement = database_
						.prepareStatement("INSERT INTO Certificate (cert_name, cert_issuer, identity_name, key_identifier, not_before, not_after, certificate_data) "
								+ "values (?, ?, ?, ?, datetime(?, 'unixepoch'), datetime(?, 'unixepoch'), ?)");
				statement.setString(1, certificateName.toUri());
	
				Name signerName = net.named_data.jndn.KeyLocator.getFromSignature(
						certificate.getSignature()).getKeyName();
				statement.setString(2, signerName.toUri());
	
				String keyId = keyName.get(-1).toEscapedString();
				Name identity = keyName.getPrefix(-1);
				statement.setString(3, identity.toUri());
				statement.setString(4, keyId);
	
				// Convert from milliseconds to seconds since 1/1/1970.
				statement.setLong(5,
						(long) (Math.Floor(certificate.getNotBefore() / 1000.0d)));
				statement.setLong(6,
						(long) (Math.Floor(certificate.getNotAfter() / 1000.0d)));
	
				// wireEncode returns the cached encoding if available.
				statement.setBytes(7, certificate.wireEncode().getImmutableArray());
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Get a certificate from the identity storage.
		/// </summary>
		///
		/// <param name="certificateName">The name of the requested certificate.</param>
		/// <returns>The requested certificate.</returns>
		/// <exception cref="System.Security.SecurityException">if the certificate doesn't exist.</exception>
		public sealed override IdentityCertificate getCertificate(Name certificateName) {
			try {
				PreparedStatement statement;
				statement = database_.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getCertificate);
				statement.setString(1, certificateName.toUri());
	
				IdentityCertificate certificate = new IdentityCertificate();
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult()) {
						try {
							certificate.wireDecode(new Blob(result
									.getBytes("certificate_data"), false));
						} catch (EncodingException ex) {
							throw new SecurityException(
									"BasicIdentityStorage: Error decoding certificate data: "
											+ ex);
						}
					} else
						throw new SecurityException(
								"BasicIdentityStorage::getKey: The key certificate not exist");
				} finally {
					statement.close();
				}
	
				return certificate;
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Get the TPM locator associated with this storage.
		/// </summary>
		///
		/// <returns>The TPM locator.</returns>
		/// <exception cref="System.Security.SecurityException">if the TPM locator doesn't exist.</exception>
		public sealed override String getTpmLocator() {
			try {
				SqlCommand statement = database_.CreateCommand();
				try {
					SqlDataReader result = statement.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getTpmLocator);
	
					if (result.NextResult())
						return (string)result["tpm_locator"];
					else
						throw new SecurityException(
								"BasicIdentityStorage::getTpmLocator: TPM info does not exist");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/*****************************************
		 *           Get/Set Default             *
		 *****************************************/
	
		/// <summary>
		/// Get the default identity.
		/// </summary>
		///
		/// <returns>The name of default identity.</returns>
		/// <exception cref="System.Security.SecurityException">if the default identity is not set.</exception>
		public sealed override Name getDefaultIdentity() {
			try {
				SqlCommand statement = database_.CreateCommand();
				try {
					SqlDataReader result = statement
							.executeQuery(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getDefaultIdentity);
	
					if (result.NextResult())
						return new Name((string)result["identity_name"]);
					else
						throw new SecurityException(
								"BasicIdentityStorage.getDefaultIdentity: The default identity is not defined");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Get the default key name for the specified identity.
		/// </summary>
		///
		/// <param name="identityName">The identity name.</param>
		/// <returns>The default key name.</returns>
		/// <exception cref="System.Security.SecurityException">if the default key name for the identity is not set.</exception>
		public sealed override Name getDefaultKeyNameForIdentity(Name identityName) {
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getDefaultKeyNameForIdentity);
				statement.setString(1, identityName.toUri());
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return new Name(identityName).append((string)result["key_identifier"]);
					else
						throw new SecurityException(
								"BasicIdentityStorage.getDefaultKeyNameForIdentity: The default key for the identity is not defined");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Get the default certificate name for the specified key.
		/// </summary>
		///
		/// <param name="keyName">The key name.</param>
		/// <returns>The default certificate name.</returns>
		/// <exception cref="System.Security.SecurityException">if the default certificate name for the key nameis not set.</exception>
		public sealed override Name getDefaultCertificateNameForKey(Name keyName) {
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getDefaultCertificateNameForKey);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return new Name((string)result["cert_name"]);
					else
						throw new SecurityException(
								"BasicIdentityStorage.getDefaultCertificateNameForKey: The default certificate for the key name is not defined");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Append all the identity names to the nameList.
		/// </summary>
		///
		/// <param name="nameList">Append result names to nameList.</param>
		/// <param name="isDefault"></param>
		public override void getAllIdentities(ArrayList nameList, bool isDefault) {
			try {
				String sql = (isDefault) ? net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllIdentities_default_true
						: net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllIdentities_default_false;
				PreparedStatement statement = database_.prepareStatement(sql);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					while (result.NextResult())
						ILOG.J2CsMapping.Collections.Collections.Add(nameList,new Name((string)result["identity_name"]));
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Append all the key names of a particular identity to the nameList.
		/// </summary>
		///
		/// <param name="identityName">The identity name to search for.</param>
		/// <param name="nameList">Append result names to nameList.</param>
		/// <param name="isDefault"></param>
		public override void getAllKeyNamesOfIdentity(Name identityName, ArrayList nameList,
				bool isDefault) {
			try {
				String sql = (isDefault) ? net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllKeyNamesOfIdentity_default_true
						: net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllKeyNamesOfIdentity_default_false;
				PreparedStatement statement = database_.prepareStatement(sql);
				statement.setString(1, identityName.toUri());
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					while (result.NextResult())
						ILOG.J2CsMapping.Collections.Collections.Add(nameList,new Name(identityName).append((string)result["key_identifier"]));
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Append all the certificate names of a particular key name to the nameList.
		/// </summary>
		///
		/// <param name="keyName">The key name to search for.</param>
		/// <param name="nameList">Append result names to nameList.</param>
		/// <param name="isDefault"></param>
		public override void getAllCertificateNamesOfKey(Name keyName, ArrayList nameList,
				bool isDefault) {
			try {
				String sql = (isDefault) ? net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllCertificateNamesOfKey_default_true
						: net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.SELECT_getAllCertificateNamesOfKey_default_false;
				PreparedStatement statement = database_.prepareStatement(sql);
				statement.setString(1, keyName.getPrefix(-1).toUri());
				statement.setString(2, keyName.get(-1).toEscapedString());
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					while (result.NextResult())
						ILOG.J2CsMapping.Collections.Collections.Add(nameList,new Name((string)result["cert_name"]));
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Set the default identity.  If the identityName does not exist, then clear
		/// the default identity so that getDefaultIdentity() throws an exception.
		/// </summary>
		///
		/// <param name="identityName">The default identity name.</param>
		public sealed override void setDefaultIdentity(Name identityName) {
			try {
				// Reset the previous default identity.
				PreparedStatement statement = database_
						.prepareStatement("UPDATE Identity SET default_identity=0 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultIdentity_reset);
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				// Set the current default identity.
				statement = database_
						.prepareStatement("UPDATE Identity SET default_identity=1 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultIdentity_set);
				statement.setString(1, identityName.toUri());
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Set a key as the default key of an identity. The identity name is inferred
		/// from keyName.
		/// </summary>
		///
		/// <param name="keyName">The name of the key.</param>
		/// <param name="identityNameCheck"></param>
		public sealed override void setDefaultKeyNameForIdentity(Name keyName,
				Name identityNameCheck) {
			checkSetDefaultKeyNameForIdentity(keyName, identityNameCheck);
	
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				// Reset the previous default Key.
				PreparedStatement statement = database_
						.prepareStatement("UPDATE Key SET default_key=0 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultKeyNameForIdentity_reset);
				statement.setString(1, identityName.toUri());
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				// Set the current default Key.
				statement = database_
						.prepareStatement("UPDATE Key SET default_key=1 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultKeyNameForIdentity_set);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Set the default key name for the specified identity.
		/// </summary>
		///
		/// <param name="keyName">The key name.</param>
		/// <param name="certificateName">The certificate name.</param>
		public sealed override void setDefaultCertificateNameForKey(Name keyName,
				Name certificateName) {
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				// Reset the previous default Certificate.
				PreparedStatement statement = database_
						.prepareStatement("UPDATE Certificate SET default_cert=0 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultCertificateNameForKey_reset);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				// Set the current default Certificate.
				statement = database_
						.prepareStatement("UPDATE Certificate SET default_cert=1 WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_setDefaultCertificateNameForKey_set);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
				statement.setString(3, certificateName.toUri());
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/*****************************************
		 *            Delete Methods             *
		 *****************************************/
	
		/// <summary>
		/// Delete a certificate.
		/// </summary>
		///
		/// <param name="certificateName">The certificate name.</param>
		public override void deleteCertificateInfo(Name certificateName) {
			if (certificateName.size() == 0)
				return;
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("DELETE FROM Certificate WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deleteCertificateInfo);
				statement.setString(1, certificateName.toUri());
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Delete a public key and related certificates.
		/// </summary>
		///
		/// <param name="keyName">The key name.</param>
		public override void deletePublicKeyInfo(Name keyName) {
			if (keyName.size() == 0)
				return;
	
			String keyId = keyName.get(-1).toEscapedString();
			Name identityName = keyName.getPrefix(-1);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("DELETE FROM Certificate WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deletePublicKeyInfo);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				statement = database_.prepareStatement("DELETE FROM Key WHERE "
						+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deletePublicKeyInfo);
				statement.setString(1, identityName.toUri());
				statement.setString(2, keyId);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Delete an identity and related public keys and certificates.
		/// </summary>
		///
		/// <param name="identityName">The identity name.</param>
		public override void deleteIdentityInfo(Name identityName) {
			String identity = identityName.toUri();
	
			try {
				PreparedStatement statement = database_
						.prepareStatement("DELETE FROM Certificate WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deleteIdentityInfo);
				statement.setString(1, identity);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				statement = database_.prepareStatement("DELETE FROM Key WHERE "
						+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deleteIdentityInfo);
				statement.setString(1, identity);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
	
				statement = database_
						.prepareStatement("DELETE FROM Identity WHERE "
								+ net.named_data.jndn.security.identity.Sqlite3IdentityStorageBase.WHERE_deleteIdentityInfo);
				statement.setString(1, identity);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new SecurityException("BasicIdentityStorage: SQLite error: "
						+ exception);
			}
		}
	
		internal SqlConnection database_;
	}
}
