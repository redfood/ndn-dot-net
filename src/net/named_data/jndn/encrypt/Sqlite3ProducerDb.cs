// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
//
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2015-2016 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn.encrypt {
	
	using ILOG.J2CsMapping.Util.Logging;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Data.SqlClient;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// Sqlite3ProducerDb extends ProducerDb to implement storage of keys for the
	/// producer using SQLite3. It contains one table that maps time slots (to the
	/// nearest hour) to the content key created for that time slot.
	/// </summary>
	///
	/// @note This class is an experimental feature. The API may change.
	public class Sqlite3ProducerDb : Sqlite3ProducerDbBase {
		/// <summary>
		/// Create an Sqlite3ProducerDb to use the given SQLite3 file.
		/// </summary>
		///
		/// <param name="databaseFilePath">The path of the SQLite file.</param>
		/// <exception cref="ProducerDb.Error">for a database error.</exception>
		public Sqlite3ProducerDb(String databaseFilePath) {
			this.database_ = null;
			try {
				ILOG.J2CsMapping.Reflect.Helper.GetNativeType("org.sqlite.JDBC");
			} catch (TypeLoadException ex) {
				// We don't expect this to happen.
				ILOG.J2CsMapping.Util.Logging.Logger.getLogger(typeof(Sqlite3ProducerDb).FullName).log(
						ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
				return;
			}
	
			try {
				database_ = System.Data.SqlClient.DriverManager.getConnection("jdbc:sqlite:"
						+ databaseFilePath);
	
				SqlCommand statement = database_.CreateCommand();
				// Use "try/finally instead of "try-with-resources" or "using" which are
				// not supported before Java 7.
				try {
					// Initialize database specific tables.
					statement.executeUpdate(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.INITIALIZATION1);
					statement.executeUpdate(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.INITIALIZATION2);
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new ProducerDb.Error("Sqlite3ProducerDb: SQLite error: "
						+ exception);
			}
		}
	
		/// <summary>
		/// Check if a content key exists for the hour covering timeSlot.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <returns>True if there is a content key for timeSlot.</returns>
		/// <exception cref="ProducerDb.Error">for a database error.</exception>
		public override bool hasContentKey(double timeSlot) {
			int fixedTimeSlot = net.named_data.jndn.encrypt.ProducerDb.getFixedTimeSlot(timeSlot);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.SELECT_hasContentKey);
				statement.setInt(1, fixedTimeSlot);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return true;
					else
						return false;
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new ProducerDb.Error(
						"Sqlite3ProducerDb.hasContentKey: SQLite error: "
								+ exception);
			}
		}
	
		/// <summary>
		/// Get the content key for the hour covering timeSlot.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <returns>A Blob with the encoded key.</returns>
		/// <exception cref="ProducerDb.Error">if there is no key covering timeSlot or otherdatabase error.</exception>
		public override Blob getContentKey(double timeSlot) {
			int fixedTimeSlot = net.named_data.jndn.encrypt.ProducerDb.getFixedTimeSlot(timeSlot);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.SELECT_getContentKey);
				statement.setInt(1, fixedTimeSlot);
	
				try {
					SqlDataReader result = statement.executeQuery();
	
					if (result.NextResult())
						return new Blob(result.getBytes(1), false);
					else
						throw new ProducerDb.Error(
								"Sqlite3ProducerDb.getContentKey: Cannot get the key from the database");
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new ProducerDb.Error(
						"Sqlite3ProducerDb.getContentKey: SQLite error: "
								+ exception);
			}
		}
	
		/// <summary>
		/// Add key as the content key for the hour covering timeSlot.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="key">The encoded key.</param>
		/// <exception cref="ProducerDb.Error">if a key for the same hour already exists in thedatabase, or other database error.</exception>
		public override void addContentKey(double timeSlot, Blob key) {
			int fixedTimeSlot = net.named_data.jndn.encrypt.ProducerDb.getFixedTimeSlot(timeSlot);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.INSERT_addContentKey);
				statement.setInt(1, fixedTimeSlot);
				statement.setBytes(2, key.getImmutableArray());
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new ProducerDb.Error(
						"Sqlite3ProducerDb.addContentKey: SQLite error: "
								+ exception);
			}
		}
	
		/// <summary>
		/// Delete the content key for the hour covering timeSlot. If there is no key
		/// for the time slot, do nothing.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <exception cref="ProducerDb.Error">for a database error.</exception>
		public override void deleteContentKey(double timeSlot) {
			int fixedTimeSlot = net.named_data.jndn.encrypt.ProducerDb.getFixedTimeSlot(timeSlot);
	
			try {
				PreparedStatement statement = database_
						.prepareStatement(net.named_data.jndn.encrypt.Sqlite3ProducerDbBase.DELETE_deleteContentKey);
				statement.setInt(1, fixedTimeSlot);
	
				try {
					statement.executeUpdate();
				} finally {
					statement.close();
				}
			} catch (SQLException exception) {
				throw new ProducerDb.Error(
						"Sqlite3ProducerDb.deleteContentKey: SQLite error: "
								+ exception);
			}
		}
	
		internal SqlConnection database_;
	}
}
