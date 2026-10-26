import React, { useState, useEffect } from 'react';
import { Users, X, Upload, CheckCircle, AlertTriangle, HelpCircle, Search, Filter, RefreshCw } from 'lucide-react';
import api from '../api';

export default function RosterReconciliationModal({ isOpen, onClose, examClass }) {
  const [file, setFile] = useState(null);
  const [isDragging, setIsDragging] = useState(false);
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState(null);
  const [statusFilter, setStatusFilter] = useState('All');
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen && examClass) {
      setFile(null);
      setError('');
      fetchReport();
    }
  }, [isOpen, examClass]);

  const fetchReport = async () => {
    if (!examClass) return;
    setLoading(true);
    try {
      const res = await api.get(`/exam-classes/${examClass.id}/roster-report`);
      setReport(res.data);
    } catch (err) {
      setReport(null);
    } finally {
      setLoading(false);
    }
  };

  const handleUploadAndReconcile = async (e) => {
    e.preventDefault();
    if (!file || !examClass) return;

    setLoading(true);
    setError('');

    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await api.post(`/exam-classes/${examClass.id}/reconcile-roster`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setReport(res.data);
      setFile(null);
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'Lỗi khi đối soát danh sách FAP. Hãy kiểm tra định dạng file Excel.');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen || !examClass) return null;

  const filteredItems = (report?.items || []).filter((item) => {
    const matchesStatus = statusFilter === 'All' || item.status.toLowerCase() === statusFilter.toLowerCase();
    const matchesSearch =
      (item.studentId || '').toLowerCase().includes(search.toLowerCase()) ||
      (item.studentName || '').toLowerCase().includes(search.toLowerCase());
    return matchesStatus && matchesSearch;
  });

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.7)',
        backdropFilter: 'blur(6px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1200,
        padding: '1rem'
      }}
    >
      <div
        className="glass-panel fade-in"
        style={{
          width: '100%',
          maxWidth: '920px',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          border: '1px solid var(--panel-border)',
          overflow: 'hidden'
        }}
      >
        {/* Modal Header */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            padding: '1.2rem 1.5rem',
            borderBottom: '1px solid var(--panel-border)',
            background: 'rgba(255, 255, 255, 0.02)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                background: 'rgba(59, 130, 246, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#3b82f6'
              }}
            >
              <Users size={20} />
            </div>
            <div>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                Đối Soát Danh Sách Thi FAP (Roster Reconciliation)
              </h3>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                Lớp thi: <b style={{ color: 'var(--color-primary)' }}>{examClass.classCode}</b> - {examClass.subjectCode}
              </span>
            </div>
          </div>

          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              padding: '0.4rem',
              borderRadius: '6px'
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Upload Excel Roster Section */}
        <div
          style={{
            padding: '1rem 1.5rem',
            background: 'rgba(255, 255, 255, 0.01)',
            borderBottom: '1px solid var(--panel-border)'
          }}
        >
          <form
            onSubmit={handleUploadAndReconcile}
            style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap' }}
          >
            <div
              onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
              onDragLeave={() => setIsDragging(false)}
              onDrop={(e) => {
                e.preventDefault();
                setIsDragging(false);
                if (e.dataTransfer.files?.[0]) setFile(e.dataTransfer.files[0]);
              }}
              style={{
                flex: 1,
                minWidth: '280px',
                border: isDragging ? '2px dashed var(--color-primary)' : '1px dashed var(--panel-border)',
                borderRadius: '8px',
                padding: '0.6rem 1rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
                background: isDragging ? 'rgba(242,113,33,0.1)' : 'rgba(0,0,0,0.2)',
                cursor: 'pointer'
              }}
              onClick={() => document.getElementById('fap-roster-input').click()}
            >
              <Upload size={18} style={{ color: 'var(--color-primary)' }} />
              <div style={{ flex: 1, fontSize: '0.85rem' }}>
                {file ? (
                  <span style={{ fontWeight: 600, color: 'var(--text-main)' }}>{file.name}</span>
                ) : (
                  <span style={{ color: 'var(--text-muted)' }}>Chọn hoặc kéo thả file danh sách FAP (.xlsx)...</span>
                )}
              </div>
              <input
                id="fap-roster-input"
                type="file"
                accept=".xlsx"
                style={{ display: 'none' }}
                onChange={(e) => {
                  if (e.target.files?.[0]) setFile(e.target.files[0]);
                }}
              />
            </div>

            <button
              type="submit"
              disabled={!file || loading}
              className="btn btn-primary btn-sm"
              style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', padding: '0.6rem 1.2rem' }}
            >
              <RefreshCw size={14} className={loading ? 'spin' : ''} />
              {loading ? 'Đang đối soát...' : 'Đối Soát Ngay'}
            </button>
          </form>

          {error && (
            <div style={{ marginTop: '0.75rem', padding: '0.6rem 0.8rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '6px', color: '#ef4444', fontSize: '0.85rem' }}>
              {error}
            </div>
          )}
        </div>

        {/* Modal Body */}
        <div style={{ padding: '1.25rem 1.5rem', overflowY: 'auto', flex: 1 }}>
          {!report ? (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
              <Users size={48} style={{ strokeWidth: 1.2, margin: '0 auto 1rem', display: 'block', color: 'var(--text-muted)' }} />
              <p style={{ fontSize: '1rem', fontWeight: 600, color: 'var(--text-main)' }}>Chưa có dữ liệu đối soát FAP</p>
              <p style={{ fontSize: '0.85rem' }}>Hãy tải file Excel danh sách lớp từ hệ thống FAP (.xlsx) ở trên để đối chiếu.</p>
            </div>
          ) : (
            <>
              {/* Metrics Cards */}
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                  gap: '0.75rem',
                  marginBottom: '1.25rem'
                }}
              >
                <div style={{ background: 'rgba(255, 255, 255, 0.03)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '0.75rem' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>DS FAP</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 700 }}>{report.totalStudentsInRoster}</div>
                </div>
                <div style={{ background: 'rgba(255, 255, 255, 0.03)', border: '1px solid var(--panel-border)', borderRadius: '8px', padding: '0.75rem' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.2rem' }}>Bài nộp đã nhận</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 700 }}>{report.totalSubmissions}</div>
                </div>
                <div style={{ background: 'rgba(34, 197, 94, 0.08)', border: '1px solid rgba(34, 197, 94, 0.25)', borderRadius: '8px', padding: '0.75rem' }}>
                  <div style={{ fontSize: '0.75rem', color: '#10b981', marginBottom: '0.2rem' }}>Khớp hợp lệ</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 800, color: '#10b981' }}>{report.matchedCount}</div>
                </div>
                <div style={{ background: 'rgba(239, 68, 68, 0.08)', border: '1px solid rgba(239, 68, 68, 0.25)', borderRadius: '8px', padding: '0.75rem' }}>
                  <div style={{ fontSize: '0.75rem', color: '#ef4444', marginBottom: '0.2rem' }}>Vắng thi (Absent)</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 800, color: '#ef4444' }}>{report.absentCount}</div>
                </div>
                <div style={{ background: 'rgba(168, 85, 247, 0.08)', border: '1px solid rgba(168, 85, 247, 0.25)', borderRadius: '8px', padding: '0.75rem' }}>
                  <div style={{ fontSize: '0.75rem', color: '#a855f7', marginBottom: '0.2rem' }}>Sai MSSV / Lạ</div>
                  <div style={{ fontSize: '1.2rem', fontWeight: 800, color: '#a855f7' }}>{report.unrecognizedCount}</div>
                </div>
              </div>

              {/* Filters & Search Bar */}
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: '1rem',
                  flexWrap: 'wrap',
                  gap: '0.75rem'
                }}
              >
                <div style={{ display: 'flex', gap: '0.35rem' }}>
                  {['All', 'Matched', 'Absent', 'Unrecognized'].map((st) => (
                    <button
                      key={st}
                      type="button"
                      onClick={() => setStatusFilter(st)}
                      className={`btn btn-sm ${statusFilter === st ? 'btn-primary' : 'btn-secondary'}`}
                      style={{ fontSize: '0.75rem', padding: '0.25rem 0.6rem' }}
                    >
                      {st === 'All' ? 'Tất cả' : st === 'Matched' ? 'Khớp' : st === 'Absent' ? 'Vắng thi' : 'Không có trong DS'}
                    </button>
                  ))}
                </div>

                <div style={{ position: 'relative', width: '220px' }}>
                  <Search size={14} style={{ position: 'absolute', left: '10px', top: '9px', color: 'var(--text-muted)' }} />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Tìm MSSV hoặc Tên..."
                    className="form-control"
                    style={{ fontSize: '0.8rem', padding: '0.3rem 0.6rem 0.3rem 1.8rem', borderRadius: '6px' }}
                  />
                </div>
              </div>

              {/* Student Table */}
              <div style={{ overflowX: 'auto', border: '1px solid var(--panel-border)', borderRadius: '8px' }}>
                <table className="custom-table" style={{ margin: 0 }}>
                  <thead>
                    <tr>
                      <th>MSSV</th>
                      <th>Họ và Tên</th>
                      <th>Trạng thái FAP</th>
                      <th>Nộp bài</th>
                      <th>Ghi chú nghiệp vụ</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredItems.length === 0 ? (
                      <tr>
                        <td colSpan="5" style={{ textAlign: 'center', padding: '1.5rem', color: 'var(--text-muted)' }}>
                          Không tìm thấy sinh viên nào phù hợp bộ lọc.
                        </td>
                      </tr>
                    ) : (
                      filteredItems.map((item, idx) => {
                        const statusColors = {
                          matched: { bg: 'rgba(34, 197, 94, 0.1)', color: '#16a34a' },
                          absent: { bg: 'rgba(239, 68, 68, 0.1)', color: '#dc2626' },
                          unrecognized: { bg: 'rgba(168, 85, 247, 0.1)', color: '#9333ea' }
                        }[item.status.toLowerCase()] || { bg: 'rgba(255,255,255,0.05)', color: 'var(--text-muted)' };

                        return (
                          <tr key={idx}>
                            <td style={{ fontWeight: 700 }}>{item.studentId}</td>
                            <td>{item.studentName || '-'}</td>
                            <td>
                              <span
                                style={{
                                  fontSize: '0.75rem',
                                  fontWeight: 700,
                                  padding: '0.2rem 0.5rem',
                                  borderRadius: '4px',
                                  background: statusColors.bg,
                                  color: statusColors.color
                                }}
                              >
                                {item.status}
                              </span>
                            </td>
                            <td>
                              {item.hasSubmission ? (
                                <span style={{ color: '#10b981', display: 'flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.8rem', fontWeight: 600 }}>
                                  <CheckCircle size={14} /> Đã nộp
                                </span>
                              ) : (
                                <span style={{ color: '#ef4444', display: 'flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.8rem', fontWeight: 600 }}>
                                  <AlertTriangle size={14} /> Chưa nộp
                                </span>
                              )}
                            </td>
                            <td style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                              {item.notes}
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>

        {/* Modal Footer */}
        <div
          style={{
            padding: '0.75rem 1.5rem',
            borderTop: '1px solid var(--panel-border)',
            background: 'rgba(255, 255, 255, 0.02)',
            display: 'flex',
            justifyContent: 'flex-end'
          }}
        >
          <button type="button" onClick={onClose} className="btn btn-secondary btn-sm">
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}
