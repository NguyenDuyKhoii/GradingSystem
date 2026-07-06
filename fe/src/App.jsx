import React, { useState, useEffect } from 'react';
import { LogOut, User, Award, GraduationCap } from 'lucide-react';
import Login from './components/Login';
import AcademicDashboard from './components/AcademicDashboard';
import LecturerDashboard from './components/LecturerDashboard';
import GradingView from './components/GradingView';

export default function App() {
  const [user, setUser] = useState(null);
  const [currentScreen, setCurrentScreen] = useState('login'); // login, dashboard, grading
  
  // Grading view state
  const [gradingSubmissionId, setGradingSubmissionId] = useState(null);
  const [gradingExamClassId, setGradingExamClassId] = useState(null);

  useEffect(() => {
    // Check if user is logged in
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      const parsed = JSON.parse(storedUser);
      setUser(parsed);
      setCurrentScreen('dashboard');
    }
  }, []);

  const handleLoginSuccess = (userData) => {
    setUser(userData);
    setCurrentScreen('dashboard');
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
    setCurrentScreen('login');
  };

  const handleGradeSelect = (submissionId, examClassId) => {
    setGradingSubmissionId(submissionId);
    setGradingExamClassId(examClassId);
    setCurrentScreen('grading');
  };

  return (
    <div className="app-container">
      {/* Navbar */}
      <nav className="navbar">
        <div className="logo-container">
          <GraduationCap size={28} style={{ color: 'var(--color-primary)' }} />
          <span className="logo-text">FPTU PE Grading</span>
        </div>

        {user && (
          <div className="nav-links">
            <div className="user-badge">
              <User size={14} style={{ color: 'var(--color-primary)' }} />
              <span style={{ fontWeight: 500 }}>{user.fullName}</span>
              <span className={`role-tag ${user.role === 'AcademicStaff' ? 'staff' : ''}`}>
                {user.role === 'AcademicStaff' ? 'Staff' : 'Lecturer'}
              </span>
            </div>
            
            <button onClick={handleLogout} className="btn btn-secondary btn-sm" style={{ gap: '0.25rem', padding: '0.4rem 0.8rem' }}>
              <LogOut size={14} /> Logout
            </button>
          </div>
        )}
      </nav>

      {/* Main Container */}
      <main style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
        {currentScreen === 'login' && (
          <Login onLoginSuccess={handleLoginSuccess} />
        )}

        {currentScreen === 'dashboard' && user && (
          user.role === 'AcademicStaff' ? (
            <AcademicDashboard />
          ) : (
            <LecturerDashboard onGradeSelect={handleGradeSelect} />
          )
        )}

        {currentScreen === 'grading' && (
          <GradingView
            submissionId={gradingSubmissionId}
            examClassId={gradingExamClassId}
            onBack={() => setCurrentScreen('dashboard')}
          />
        )}
      </main>
    </div>
  );
}
