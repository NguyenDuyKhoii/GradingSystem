import React, { useState } from 'react';
import { KeyRound, Mail, User, ShieldCheck, LogIn, UserPlus } from 'lucide-react';
import { userApi } from '../api';

export default function Login({ onLoginSuccess }) {
  const [isRegister, setIsRegister] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [role, setRole] = useState('Lecturer');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);

    try {
      if (isRegister) {
        // Register flow
        await userApi.post('/Auth/register', {
          username,
          password,
          role,
          email,
          fullName,
        });
        setMessage('Registration successful! Please login.');
        setIsRegister(false);
        setPassword('');
      } else {
        // Login flow
        const response = await userApi.post('/Auth/login', {
          username,
          password,
        });
        const { token, role: userRole, fullName: name, id, username: uname } = response.data;
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify({ token, role: userRole, fullName: name, id, username: uname }));
        onLoginSuccess({ token, role: userRole, fullName: name, id, username: uname });
      }
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.detail || err.response?.data?.title || 'An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%', flex: 1, padding: '1.5rem', overflowY: 'auto' }}>
      <div className="glass-panel fade-in" style={{ width: '100%', maxWidth: '420px', overflow: 'hidden' }}>
        <div className="card-header" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', justifyContent: 'center', background: 'rgba(255, 255, 255, 0.02)' }}>
          <span style={{ fontSize: '1.4rem', fontWeight: 700, color: 'var(--text-main)' }}>
            {isRegister ? 'Create Account' : 'Login'}
          </span>
        </div>
        
        <form onSubmit={handleSubmit} className="card-body">
          {error && (
            <div style={{ padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '8px', color: 'var(--color-danger)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
              {error}
            </div>
          )}

          {message && (
            <div style={{ padding: '0.75rem 1rem', background: 'rgba(16, 185, 129, 0.15)', border: '1px solid rgba(16, 185, 129, 0.3)', borderRadius: '8px', color: 'var(--color-success)', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
              {message}
            </div>
          )}

          {isRegister && (
            <>
              <div className="form-group">
                <label className="form-label">Full Name</label>
                <div style={{ position: 'relative' }}>
                  <User size={16} style={{ position: 'absolute', left: '12px', top: '14px', color: 'var(--text-muted)' }} />
                  <input
                    type="text"
                    required
                    className="form-control"
                    style={{ paddingLeft: '2.5rem' }}
                    placeholder="Nguyen Van A"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Email</label>
                <div style={{ position: 'relative' }}>
                  <Mail size={16} style={{ position: 'absolute', left: '12px', top: '14px', color: 'var(--text-muted)' }} />
                  <input
                    type="email"
                    required
                    className="form-control"
                    style={{ paddingLeft: '2.5rem' }}
                    placeholder="anv@fpt.edu.vn"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">System Role</label>
                <div style={{ position: 'relative' }}>
                  <ShieldCheck size={16} style={{ position: 'absolute', left: '12px', top: '14px', color: 'var(--text-muted)' }} />
                  <select
                    className="form-control"
                    style={{ paddingLeft: '2.5rem' }}
                    value={role}
                    onChange={(e) => setRole(e.target.value)}
                  >
                    <option value="Lecturer">Lecturer (Chấm thi)</option>
                    <option value="AcademicStaff">Academic Staff (Khảo thí / Giáo vụ)</option>
                  </select>
                </div>
              </div>
            </>
          )}

          <div className="form-group">
            <label className="form-label">Username</label>
            <input
              type="text"
              required
              className="form-control"
              placeholder="anvse12345"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1.75rem' }}>
            <label className="form-label">Password</label>
            <input
              type="password"
              required
              className="form-control"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          <button type="submit" disabled={loading} className="btn btn-primary" style={{ width: '100%', marginBottom: '1rem' }}>
            {loading ? 'Processing...' : isRegister ? <><UserPlus size={18} /> Sign Up</> : <><LogIn size={18} /> Sign In</>}
          </button>

          <div style={{ textAlign: 'center', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
            {isRegister ? (
              <p>
                Already have an account?{' '}
                <span onClick={() => setIsRegister(false)} style={{ color: 'var(--color-primary)', cursor: 'pointer', fontWeight: 500 }}>
                  Sign In
                </span>
              </p>
            ) : (
              <p>
                New user?{' '}
                <span onClick={() => setIsRegister(true)} style={{ color: 'var(--color-primary)', cursor: 'pointer', fontWeight: 500 }}>
                  Create an account
                </span>
              </p>
            )}
          </div>
        </form>
      </div>
    </div>
  );
}
