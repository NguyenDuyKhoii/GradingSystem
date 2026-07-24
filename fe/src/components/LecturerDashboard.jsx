import React, { useState, useEffect } from 'react';
import {
  Layers,
  Search,
  ChevronLeft,
  ChevronRight,
  CheckCircle,
  Clock,
  AlertTriangle
} from 'lucide-react';
import api from '../api';

export default function LecturerDashboard({ onGradeSelect, selectedClass, setSelectedClass }) {
  const [classes, setClasses] = useState([]);

  // Submissions state
  const [submissions, setSubmissions] = useState([]);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => {
    fetchClasses();
  }, []);

  useEffect(() => {
    if (selectedClass) {
      fetchSubmissions();
    }
  }, [selectedClass, search, statusFilter, page]);

  const fetchClasses = async () => {
    try {
      const res = await api.get('/ExamClasses');
      setClasses(res.data);
    } catch (err) {
      console.error(err);
    }
  };

  const fetchSubmissions = async () => {
    try {
      const res = await api.get(`/Submissions/class/${selectedClass.id}`, {
        params: {
          searchQuery: search,
          status: statusFilter,
          pageNumber: page,
          pageSize: 5
        }
      });

      setSubmissions(res.data.items);
      setTotalPages(res.data.totalPages);
      setTotalCount(res.data.totalCount);
    } catch (err) {
      console.error(err);
    }
  };

  const getStatusIcon = (status) => {
    switch (status.toLowerCase()) {
      case 'graded':
        return (
          <CheckCircle
            size={16}
            style={{
              color: 'var(--color-success)',
              marginRight: '4px'
            }}
          />
        );

      case 'draft':
        return (
          <Clock
            size={16}
            style={{
              color: 'var(--color-warning)',
              marginRight: '4px'
            }}
          />
        );

      default:
        return (
          <AlertTriangle
            size={16}
            style={{
              color: 'var(--text-muted)',
              marginRight: '4px'
            }}
          />
        );
    }
  };

  const getGradedCount = () => {
    return submissions.filter(
      (s) => s.status.toLowerCase() === 'graded'
    ).length;
  };

  return (
    <div className="dashboard-container fade-in">
      {!selectedClass ? (
        // Classes List Screen
        <>
          <div className="page-header">
            <div>
              <h1 style={{ fontSize: '1.75rem', fontWeight: 700 }}>
                Lecturer Dashboard
              </h1>
              <p
                style={{
                  color: 'var(--text-muted)',
                  fontSize: '0.95rem'
                }}
              >
                Select an assigned exam slot class to begin grading.
              </p>
            </div>
          </div>

          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))',
              gap: '1.5rem',
              marginTop: '1rem'
            }}
          >
            {classes.length === 0 ? (
              <div
                className="glass-panel"
                style={{
                  gridColumn: '1/-1',
                  textAlign: 'center',
                  padding: '4rem 2rem',
                  color: 'var(--text-muted)'
                }}
              >
                <Layers
                  size={48}
                  style={{
                    strokeWidth: 1,
                    marginBottom: '1rem',
                    color: 'var(--text-muted)'
                  }}
                />
                <p>You have no assigned exam classes for grading yet.</p>
              </div>
            ) : (
              classes.map((c) => (
                <div
                  key={c.id}
                  onClick={() => setSelectedClass(c)}
                  className="glass-panel fade-in"
                  style={{
                    cursor: 'pointer',
                    overflow: 'hidden',
                    border: '1px solid rgba(255,255,255,0.06)'
                  }}
                >
                  <div
                    style={{
                      padding: '1.25rem 1.5rem',
                      background: 'rgba(242, 113, 33, 0.05)',
                      borderBottom: '1px solid var(--panel-border)',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center'
                    }}
                  >
                    <span
                      style={{
                        fontWeight: 700,
                        fontSize: '1.15rem',
                        color: 'var(--color-primary)'
                      }}
                    >
                      {c.classCode}
                    </span>

                    <span
                      style={{
                        fontSize: '0.8rem',
                        color: 'var(--text-muted)',
                        background: 'rgba(255,255,255,0.05)',
                        padding: '0.2rem 0.5rem',
                        borderRadius: '4px'
                      }}
                    >
                      {c.semester}
                    </span>
                  </div>

                  <div className="card-body">
                    <p
                      style={{
                        fontWeight: 500,
                        fontSize: '1rem',
                        marginBottom: '0.5rem'
                      }}
                    >
                      {c.subjectCode} - {c.subjectName}
                    </p>

                    <div style={{ marginTop: '1.25rem' }}>
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          fontSize: '0.85rem',
                          color: 'var(--text-muted)',
                          marginBottom: '0.25rem'
                        }}
                      >
                        <span>Grading Progress</span>
                        <span>{c.status}</span>
                      </div>

                      <div className="progress-container">
                        <div
                          className="progress-bar"
                          style={{
                            width: c.status === 'Completed' ? '100%' : '30%'
                          }}
                        />
                      </div>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      ) : (
        // Submissions List inside Selected Class Screen
        <div className="fade-in">
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              marginBottom: '1.5rem'
            }}
          >
            <button
              onClick={() => setSelectedClass(null)}
              className="btn btn-secondary btn-sm"
              style={{ padding: '0.4rem' }}
            >
              <ChevronLeft size={16} /> Back
            </button>

            <div>
              <span
                style={{
                  fontSize: '0.9rem',
                  color: 'var(--text-muted)'
                }}
              >
                Exam Classes /{' '}
              </span>
              <h2
                style={{
                  fontSize: '1.5rem',
                  fontWeight: 700,
                  display: 'inline'
                }}
              >
                {selectedClass.classCode} Submissions
              </h2>
            </div>
          </div>

          <div className="grid-cols-2" style={{ alignItems: 'start' }}>
            {/* Submissions List */}
            <div className="glass-panel" style={{ overflow: 'hidden' }}>
              <div
                className="card-header"
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: '1rem'
                }}
              >
                <span>Submissions ({totalCount})</span>

                <div
                  style={{
                    display: 'flex',
                    gap: '0.5rem',
                    alignItems: 'center'
                  }}
                >
                  <div style={{ position: 'relative' }}>
                    <Search
                      size={14}
                      style={{
                        position: 'absolute',
                        left: '10px',
                        top: '10px',
                        color: 'var(--text-muted)'
                      }}
                    />

                    <input
                      type="text"
                      className="form-control"
                      style={{
                        width: '160px',
                        padding: '0.35rem 0.75rem 0.35rem 2rem',
                        fontSize: '0.85rem',
                        borderRadius: '6px'
                      }}
                      placeholder="Search Student..."
                      value={search}
                      onChange={(e) => {
                        setSearch(e.target.value);
                        setPage(1);
                      }}
                    />
                  </div>

                  <select
                    className="form-control"
                    style={{
                      width: '130px',
                      padding: '0.35rem 0.75rem',
                      fontSize: '0.85rem',
                      borderRadius: '6px'
                    }}
                    value={statusFilter}
                    onChange={(e) => {
                      setStatusFilter(e.target.value);
                      setPage(1);
                    }}
                  >
                    <option value="">All Statuses</option>
                    <option value="Unassigned">Unassigned</option>
                    <option value="Draft">Draft</option>
                    <option value="Graded">Graded</option>
                  </select>
                </div>
              </div>

              <div style={{ overflowX: 'auto' }}>
                <table className="custom-table">
                  <thead>
                    <tr>
                      <th>MSSV</th>
                      <th>Student Name</th>
                      <th>File Type</th>
                      <th>Status</th>
                      <th>Score</th>
                      <th>Letter Grade</th>
                      <th>Action</th>
                    </tr>
                  </thead>

                  <tbody>
                    {submissions.length === 0 ? (
                      <tr>
                        <td
                          colSpan="7"
                          style={{
                            textAlign: 'center',
                            color: 'var(--text-muted)',
                            padding: '2rem'
                          }}
                        >
                          No submissions found.
                        </td>
                      </tr>
                    ) : (
                      submissions.map((s) => (
                        <tr key={s.id}>
                          <td style={{ fontWeight: 600 }}>{s.studentId}</td>
                          <td>{s.studentName}</td>

                          <td
                            style={{
                              textTransform: 'uppercase',
                              fontSize: '0.8rem',
                              fontWeight: 600,
                              color:
                                s.fileType === 'xlsx'
                                  ? 'var(--color-success)'
                                  : 'var(--color-secondary)'
                            }}
                          >
                            {s.fileType}
                          </td>

                          <td>
                            <div
                              style={{
                                display: 'flex',
                                alignItems: 'center'
                              }}
                            >
                              {getStatusIcon(s.status)}
                              <span style={{ fontSize: '0.85rem' }}>
                                {s.status}
                              </span>
                            </div>
                          </td>

                          <td
                            style={{
                              fontWeight: 700,
                              color:
                                s.totalScore !== null
                                  ? 'var(--color-primary)'
                                  : 'var(--text-muted)'
                            }}
                          >
                            {s.totalScore !== null
                              ? s.totalScore.toFixed(2)
                              : '-'}
                          </td>

                          <td>
                            {s.letterGrade ? (
                              <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.4rem' }}>
                                <span style={{
                                  background: 'rgba(242, 113, 33, 0.1)',
                                  color: 'var(--color-primary)',
                                  fontWeight: 800,
                                  fontSize: '0.85rem',
                                  padding: '0.15rem 0.5rem',
                                  borderRadius: '6px',
                                  border: '1px solid rgba(242, 113, 33, 0.2)'
                                }}>
                                  {s.letterGrade}
                                </span>
                                {s.isPassed !== null && (
                                  <span style={{
                                    fontSize: '0.75rem',
                                    fontWeight: 700,
                                    padding: '0.15rem 0.4rem',
                                    borderRadius: '4px',
                                    background: s.isPassed ? 'rgba(34, 197, 94, 0.1)' : 'rgba(239, 68, 68, 0.1)',
                                    color: s.isPassed ? '#166534' : '#991b1b'
                                  }}>
                                    {s.isPassed ? 'PASS' : 'FAIL'}
                                  </span>
                                )}
                              </div>
                            ) : (
                              <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>-</span>
                            )}
                          </td>

                          <td>
                            <button
                              onClick={() =>
                                onGradeSelect(s.id, selectedClass.id)
                              }
                              className="btn btn-primary btn-sm"
                            >
                              Grade
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div
                  style={{
                    padding: '1rem',
                    borderTop: '1px solid var(--panel-border)',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}
                >
                  <span
                    style={{
                      fontSize: '0.85rem',
                      color: 'var(--text-muted)'
                    }}
                  >
                    Page {page} of {totalPages}
                  </span>

                  <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button
                      disabled={page === 1}
                      onClick={() => setPage(page - 1)}
                      className="btn btn-secondary btn-sm"
                      style={{ padding: '0.3rem' }}
                    >
                      <ChevronLeft size={16} />
                    </button>

                    <button
                      disabled={page === totalPages}
                      onClick={() => setPage(page + 1)}
                      className="btn btn-secondary btn-sm"
                      style={{ padding: '0.3rem' }}
                    >
                      <ChevronRight size={16} />
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Class Details panel */}
            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '1.5rem'
              }}
            >
              <div className="glass-panel">
                <div className="card-header">Class Metrics</div>

                <div className="card-body">
                  <div
                    style={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: '1rem'
                    }}
                  >
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between'
                      }}
                    >
                      <span style={{ color: 'var(--text-muted)' }}>
                        Subject:
                      </span>
                      <span style={{ fontWeight: 600 }}>
                        {selectedClass.subjectCode}
                      </span>
                    </div>

                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between'
                      }}
                    >
                      <span style={{ color: 'var(--text-muted)' }}>Name:</span>
                      <span
                        style={{
                          fontWeight: 500,
                          textAlign: 'right'
                        }}
                      >
                        {selectedClass.subjectName}
                      </span>
                    </div>

                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between'
                      }}
                    >
                      <span style={{ color: 'var(--text-muted)' }}>
                        Assigned Lecturer:
                      </span>
                      <span style={{ fontWeight: 500 }}>
                        {selectedClass.lecturerName}
                      </span>
                    </div>

                    <hr
                      style={{
                        border: 'none',
                        borderTop: '1px solid var(--panel-border)'
                      }}
                    />

                    <div>
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          fontSize: '0.9rem',
                          marginBottom: '0.5rem'
                        }}
                      >
                        <span>Completion Rate</span>
                        <span>
                          {getGradedCount()} / {totalCount} graded
                        </span>
                      </div>

                      <div className="progress-container">
                        <div
                          className="progress-bar"
                          style={{
                            width:
                              totalCount > 0
                                ? `${(getGradedCount() / totalCount) * 100}%`
                                : '0%'
                          }}
                        />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}