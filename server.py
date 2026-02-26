import http.server
import socketserver
import sqlite3
import json
import urllib.parse
import os
import csv
import io
import sys
from datetime import datetime
import logging
import getpass
import socket

# Configuration
APP_DIR = os.path.dirname(os.path.abspath(__file__))
DB_PATH = os.path.join(APP_DIR, 'womenshealth.db')
LOG_PATH = os.path.join(APP_DIR, 'server.log')
HTML_FORM_PATH = os.path.join(APP_DIR, 'WomensHealth_DataForm.html')
HTML_VIEWER_PATH = os.path.join(APP_DIR, 'WomensHealth_Viewer.html')
LOCK_FILE = os.path.join(APP_DIR, 'server.lock')
LOCK_INFO = os.path.join(APP_DIR, 'server.info')
HOST = '127.0.0.1'
PORT = 8080

# Logging Setup
logging.basicConfig(
    filename=LOG_PATH,
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

lock_file_handle = None

def acquire_app_lock():
    global lock_file_handle
    user_info = f"{getpass.getuser()} on {socket.gethostname()}"

    try:
        if os.name == 'nt':
            import msvcrt
            lock_file_handle = open(LOCK_FILE, 'a')
            try:
                msvcrt.locking(lock_file_handle.fileno(), msvcrt.LK_NBLCK, 1)
            except OSError:
                lock_file_handle.close()
                return False
        else:
            import fcntl
            lock_file_handle = open(LOCK_FILE, 'a')
            try:
                fcntl.flock(lock_file_handle.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
            except (IOError, OSError):
                lock_file_handle.close()
                return False

        # We got the lock! Write our info.
        try:
            with open(LOCK_INFO, 'w') as f:
                f.write(user_info)
        except Exception:
            pass
            
        return True

    except Exception as e:
        logging.error(f"Error locking app: {e}")
        # Allow running if we can't lock properly due to permissions
        return True

def get_db_connection():
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    # Enable WAL mode for reliability
    conn.execute('PRAGMA journal_mode=WAL')
    return conn

def init_db():
    try:
        conn = get_db_connection()
        cursor = conn.cursor()
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS submissions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                submitted_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                session_date TEXT NOT NULL,
                client_id TEXT,
                age TEXT,
                contact_mode TEXT,
                country TEXT,
                language TEXT,
                income_source TEXT,
                visa_type TEXT,
                ethnicity TEXT,
                disability TEXT,
                chronic_illness TEXT,
                presenting_issues TEXT,
                service_provided TEXT,
                service_type TEXT,
                practitioner TEXT,
                group_type TEXT,
                evaluation_tools TEXT
            )
        ''')
        
        # Schema migration: Add staff_member if not exists
        cursor.execute("PRAGMA table_info(submissions)")
        columns = [row['name'] for row in cursor.fetchall()]
        if 'staff_member' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN staff_member TEXT")
        if 'client_status' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN client_status TEXT")
        if 'visit_number' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN visit_number TEXT")
        if 'carer' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN carer TEXT DEFAULT 'No'")
        if 'financial_hardship' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN financial_hardship TEXT DEFAULT 'No'")
        if 'social_isolation' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN social_isolation TEXT DEFAULT 'No'")
        if 'rural_postcode' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN rural_postcode TEXT DEFAULT 'No'")
        if 'lgbtiq' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN lgbtiq TEXT DEFAULT 'No'")
        if 'funding_stream' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN funding_stream TEXT")
        if 'funding_option' not in columns:
            cursor.execute("ALTER TABLE submissions ADD COLUMN funding_option TEXT")
            
        conn.commit()
        conn.close()
        logging.info("Database initialized successfully.")
    except Exception as e:
        logging.error(f"Failed to initialize database: {e}")
        sys.exit(1)

class WomensHealthHandler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        parsed_path = urllib.parse.urlparse(self.path)
        
        if parsed_path.path == '/':
            self.serve_form()
        elif parsed_path.path == '/viewer':
            self.serve_viewer()
        elif parsed_path.path == '/api/records':
            self.handle_get_records(parsed_path.query)
        elif parsed_path.path == '/api/export':
            self.handle_export(parsed_path.query)
        elif parsed_path.path == '/api/options':
            self.handle_get_options()
        elif parsed_path.path == '/api/shutdown':
            self.send_json_response(200, {"status": "ok", "message": "Server shutting down..."})
            import threading
            threading.Thread(target=self.server.shutdown).start()
            return
        else:
            self.send_error(404, "File Not Found")

    def do_POST(self):
        if self.path == '/api/submit':
            self.handle_submit()
        elif self.path == '/api/restore':
            self.handle_restore()
        else:
            self.send_error(404, "Not Found")

    def do_DELETE(self):
        parsed_path = urllib.parse.urlparse(self.path)
        # Expect path like /api/record/42
        parts = parsed_path.path.strip('/').split('/')
        if len(parts) == 3 and parts[0] == 'api' and parts[1] == 'record':
            self.handle_delete_record(parts[2])
        else:
            self.send_error(404, "Not Found")

    def handle_delete_record(self, record_id):
        try:
            rid = int(record_id)  # validate it's an integer
        except (ValueError, TypeError):
            self.send_json_response(400, {"status": "error", "message": "Invalid record ID"})
            return
        try:
            conn = get_db_connection()
            cursor = conn.cursor()
            cursor.execute("SELECT id FROM submissions WHERE id = ?", (rid,))
            if cursor.fetchone() is None:
                conn.close()
                self.send_json_response(404, {"status": "error", "message": "Record not found"})
                return
            cursor.execute("DELETE FROM submissions WHERE id = ?", (rid,))
            conn.commit()
            conn.close()
            logging.info(f"Record {rid} deleted.")
            self.send_json_response(200, {"status": "ok", "deleted_id": rid})
        except Exception as e:
            logging.error(f"Error deleting record {record_id}: {e}")
            self.send_json_response(500, {"status": "error", "message": "Failed to delete record"})

    def serve_form(self):
        try:
            if not os.path.exists(HTML_FORM_PATH):
                self.send_response(404)
                self.send_header('Content-type', 'text/plain')
                self.end_headers()
                self.wfile.write(b"Error: WomensHealth_DataForm.html not found.")
                return

            with open(HTML_FORM_PATH, 'rb') as f:
                content = f.read()
                self.send_response(200)
                self.send_header('Content-type', 'text/html; charset=utf-8')
                self.end_headers()
                self.wfile.write(content)
        except Exception as e:
            logging.error(f"Error serving form: {e}")
            self.send_error(500, "Internal Server Error")

    def handle_get_options(self):
        try:
            data_path = os.path.join(APP_DIR, 'data.json')
            if not os.path.exists(data_path):
                self.send_json_response(404, {"error": "data.json not found"})
                return
            with open(data_path, 'r', encoding='utf-8') as f:
                content = f.read()
            self.send_response(200)
            self.send_header('Content-type', 'application/json; charset=utf-8')
            self.end_headers()
            self.wfile.write(content.encode('utf-8'))
        except Exception as e:
            logging.error(f"Error serving options: {e}")
            self.send_json_response(500, {"error": "Internal Server Error"})

    def serve_viewer(self):
        try:
            if not os.path.exists(HTML_VIEWER_PATH):
                self.send_response(404)
                self.send_header('Content-type', 'text/plain')
                self.end_headers()
                self.wfile.write(b"Error: WomensHealth_Viewer.html not found.")
                return

            with open(HTML_VIEWER_PATH, 'rb') as f:
                content = f.read()
                self.send_response(200)
                self.send_header('Content-type', 'text/html; charset=utf-8')
                self.end_headers()
                self.wfile.write(content)
        except Exception as e:
            logging.error(f"Error serving viewer: {e}")
            self.send_error(500, "Internal Server Error")

    def handle_submit(self):
        try:
            content_length = int(self.headers.get('Content-Length', 0))
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data)

            # Validation
            session_date = data.get('session_date')
            if not session_date:
                self.send_json_response(400, {"status": "error", "message": "session_date is required"})
                return

            # Prepare fields (handling multi-select join with '|')
            fields = [
                'session_date', 'client_id', 'staff_member', 'client_status', 'visit_number', 'age', 'carer', 'financial_hardship', 'social_isolation', 'rural_postcode', 'lgbtiq',
                'funding_stream', 'funding_option', 'contact_mode', 
                'country', 'language', 'income_source', 'visa_type', 'ethnicity',
                'disability', 'chronic_illness', 'presenting_issues', 'service_provided',
                'service_type', 'practitioner', 'group_type', 'evaluation_tools'
            ]
            
            values = []
            for field in fields:
                val = data.get(field, "")
                if isinstance(val, list):
                    val = "|".join(val)
                values.append(val)

            conn = get_db_connection()
            cursor = conn.cursor()
            placeholders = ", ".join(["?"] * len(fields))
            query = f"INSERT INTO submissions ({', '.join(fields)}) VALUES ({placeholders})"
            cursor.execute(query, values)
            row_id = cursor.lastrowid
            conn.commit()
            conn.close()

            self.send_json_response(200, {"status": "ok", "id": row_id})
        except Exception as e:
            logging.error(f"Error in handle_submit: {e}")
            self.send_json_response(500, {"status": "error", "message": "Failed to save record"})

    def _build_where_clause(self, params):
        """Build a SQL WHERE clause + params list from a parsed query-string dict."""
        where_clauses = []
        query_params = []

        p = lambda key: params.get(key, [None])[0]

        # --- Exact / range filters ---
        if p('date_from'):
            where_clauses.append("session_date >= ?")
            query_params.append(p('date_from'))
        if p('date_to'):
            where_clauses.append("session_date <= ?")
            query_params.append(p('date_to'))
        if p('age'):
            where_clauses.append("age = ?")
            query_params.append(p('age'))
        if p('contact_mode'):
            where_clauses.append("contact_mode = ?")
            query_params.append(p('contact_mode'))

        # --- Field-specific LIKE filters ---
        like_fields = [
            'client_id', 'staff_member', 'client_status', 'visit_number', 'carer', 'financial_hardship', 'social_isolation', 'rural_postcode', 'lgbtiq', 'funding_stream', 'funding_option', 'country', 'language', 'ethnicity',
            'visa_type', 'income_source', 'disability', 'chronic_illness',
            'presenting_issues', 'service_provided', 'service_type',
            'practitioner', 'group_type', 'evaluation_tools',
        ]
        for field in like_fields:
            val = p(field)
            if val:
                where_clauses.append(f"{field} LIKE ?")
                query_params.append(f"%{val}%")

        # --- Free-text search across all text fields ---
        search = p('search')
        if search:
            search_term = f"%{search}%"
            search_clauses = [
                "client_id LIKE ?", "staff_member LIKE ?", "client_status LIKE ?", "visit_number LIKE ?",
                "age LIKE ?", "contact_mode LIKE ?", "session_date LIKE ?",
                "carer LIKE ?", "financial_hardship LIKE ?", "social_isolation LIKE ?", "rural_postcode LIKE ?", "lgbtiq LIKE ?", "funding_stream LIKE ?", "funding_option LIKE ?", "country LIKE ?",
                "language LIKE ?", "ethnicity LIKE ?", "disability LIKE ?",
                "chronic_illness LIKE ?", "presenting_issues LIKE ?",
                "service_provided LIKE ?", "service_type LIKE ?",
                "practitioner LIKE ?", "evaluation_tools LIKE ?",
                "group_type LIKE ?", "visa_type LIKE ?", "income_source LIKE ?"
            ]
            where_clauses.append("(" + " OR ".join(search_clauses) + ")")
            query_params.extend([search_term] * len(search_clauses))

        where_str = ""
        if where_clauses:
            where_str = " WHERE " + " AND ".join(where_clauses)
        return where_str, query_params

    def handle_get_records(self, query_str):
        try:
            params = urllib.parse.parse_qs(query_str)

            page = int(params.get('page', [1])[0])
            per_page = int(params.get('per_page', [50])[0])
            offset = (page - 1) * per_page

            where_str, query_params = self._build_where_clause(params)

            conn = get_db_connection()
            cursor = conn.cursor()

            # Count total
            count_query = f"SELECT COUNT(*) as total FROM submissions{where_str}"
            cursor.execute(count_query, query_params)
            total = cursor.fetchone()['total']

            # Get records
            records_query = f"SELECT * FROM submissions{where_str} ORDER BY session_date DESC, id DESC LIMIT ? OFFSET ?"
            cursor.execute(records_query, query_params + [per_page, offset])
            rows = cursor.fetchall()

            records = [dict(row) for row in rows]
            conn.close()

            self.send_json_response(200, {
                "total": total,
                "page": page,
                "per_page": per_page,
                "records": records
            })
        except Exception as e:
            logging.error(f"Error in handle_get_records: {e}")
            self.send_json_response(500, {"status": "error", "message": "Internal Database Error"})

    def handle_export(self, query_str):
        try:
            params = urllib.parse.parse_qs(query_str)

            where_str, query_params = self._build_where_clause(params)

            conn = get_db_connection()
            cursor = conn.cursor()
            records_query = f"SELECT * FROM submissions{where_str} ORDER BY session_date DESC, id DESC"
            cursor.execute(records_query, query_params)
            rows = cursor.fetchall()

            # Stream CSV response
            filename = f"womenshealth_export_{datetime.now().strftime('%Y-%m-%d')}.csv"

            self.send_response(200)
            self.send_header('Content-Type', 'text/csv; charset=utf-8')
            self.send_header('Content-Disposition', f'attachment; filename={filename}')
            self.end_headers()

            # Write BOM for Excel compatibility
            self.wfile.write(b"\xef\xbb\xbf")

            output = io.StringIO()
            writer = csv.writer(output)

            if rows:
                writer.writerow(rows[0].keys())
                for row in rows:
                    writer.writerow(list(row))

            self.wfile.write(output.getvalue().encode('utf-8'))
            conn.close()
        except Exception as e:
            logging.error(f"Error in handle_export: {e}")
            self.send_error(500, "Internal Server Error during export")

    def handle_restore(self):
        try:
            content_length = int(self.headers.get('Content-Length', 0))
            if content_length == 0:
                self.send_json_response(400, {"status": "error", "message": "No file uploaded"})
                return
                
            uploaded_db = self.rfile.read(content_length)
            
            # Simple magic bytes check for SQLite3 DB
            if not uploaded_db.startswith(b'SQLite format 3\x00'):
                self.send_json_response(400, {"status": "error", "message": "Invalid database file format"})
                return
                
            with open(DB_PATH, 'wb') as f:
                f.write(uploaded_db)
                
            logging.info("Database restored from backup.")
            self.send_json_response(200, {"status": "ok", "message": "Database restored successfully"})
        except Exception as e:
            logging.error(f"Error restoring database: {e}")
            self.send_json_response(500, {"status": "error", "message": "Internal Server Error during restore"})

    def send_json_response(self, status_code, data):
        self.send_response(status_code)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(data).encode('utf-8'))

def backup_db():
    try:
        if not os.path.exists(DB_PATH):
            return
            
        import shutil
        import glob
        
        backup_dir = os.path.join(APP_DIR, 'backups')
        if not os.path.exists(backup_dir):
            os.makedirs(backup_dir)
            
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        backup_file = os.path.join(backup_dir, f'womenshealth_backup_{timestamp}.db')
        
        shutil.copy2(DB_PATH, backup_file)
        logging.info(f"Database backed up to {backup_file}")
        
        # Keep only the last 5 copies
        backups = sorted(glob.glob(os.path.join(backup_dir, 'womenshealth_backup_*.db')))
        if len(backups) > 5:
            for old_backup in backups[:-5]:
                try:
                    os.remove(old_backup)
                    logging.info(f"Removed old backup: {old_backup}")
                except OSError:
                    pass
    except Exception as e:
        logging.error(f"Error during backup: {e}")

def run_server():
    if not acquire_app_lock():
        try:
            with open(LOCK_INFO, 'r') as f:
                current_user = f.read().strip()
        except Exception:
            current_user = "Another user"
            
        msg = f"\n======================================================\nERROR: The application is already in use by:\n[{current_user}]\n\nPlease ask them to close the application\nbefore you can start it.\n======================================================\n"
        print(msg)
        logging.error(f"App already in use by {current_user}")
        sys.exit(1)

    init_db()
    
    # Port conflict detection
    try:
        server = socketserver.TCPServer((HOST, PORT), WomensHealthHandler)
        print(f"Server started at http://{HOST}:{PORT}")
        logging.info(f"Server started at http://{HOST}:{PORT}")
        try:
            server.serve_forever()
        finally:
            backup_db()
            print("Database backed up. Server shutdown gracefully.")
            logging.info("Server graceful shutdown complete.")
    except OSError as e:
        if e.errno == 98 or e.errno == 10048: # Address already in use
            print(f"Error: Port {PORT} is already in use.")
            logging.error(f"Port {PORT} already in use.")
            sys.exit(1)
        else:
            print(f"Unexpected error: {e}")
            logging.error(f"Unexpected error: {e}")
            sys.exit(1)

if __name__ == "__main__":
    run_server()
