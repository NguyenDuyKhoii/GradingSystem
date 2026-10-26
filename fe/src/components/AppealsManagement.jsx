import React, { useState, useEffect } from 'react';
import {
  ClipboardCheck,
  Clock,
  CheckCircle,
  XCircle,
  UserCheck,
  AlertTriangle,
  Send,
  Plus,
  RefreshCw,
  Lock,
  Unlock,
  ChevronRight,
  X
} from 'lucide-react';
import api from '../api';

export default function AppealsManagement({ classes, lecturers, onRefreshClasses }) {
  const [selectedClassId, setSelectedClassId] = useState('');
  const [appeals, setAppeals] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  // Assign Reviewer Modal State
  const [assignModal, setAssignModal] = useState({
    show: false,
    appeal: null,
    reviewerLecturerId: ''
  });

  // Resolve Appeal Modal State
  const [resolveModal, setResolveModal] = useState({
    show: false,
    appeal: null,
    decision: 'Approved',
    finalScore: '',
    resolutionNotes: ''
  });

  // Create Sample Appeal Modal State (for easy testing & demonstration)
  const [createSampleModal, setCreateSampleModal] = useState({
    show: false,
    submissions: [],
    submissionId: '',
    reason: 'Em xin chấm phúc khảo do bài thi của em làm đúng đủ tiêu chí nhưng bị trừ điểm phần cú pháp.'
  });

  useEffect(() => {
    if (selectedClassId) {
      fetchAppeals();
    } else {
      setAppeals([]);
    }
  }, [selectedClassId]);

  const selectedClass = classes.find((c) => c.id === Number(selectedClassId));

  const fetchAppeals = async () => {
    if (!selectedClassId) return;
    setLoading(true);
    setError('');
    try {
      const res = await api.get(`/appeals/exam-class/${selectedClassId}`);
      setAppeals(res.data);
    } catch (err) {
      console.error(err);
      setError('Không thể tải danh sách đơn phúc khảo.');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenAppealDuration = async () => {
    if (!selectedClassId) return;
    if (!window.confirm('Bạn có chắc muốn mở tiếp nhận đơn phúc khảo trong 48 giờ cho lớp thi này?')) return;

    setError('');
    setMessage('');
    try {
      const res = await api.post(`/exam-classes/${selectedClassId}/open-appeal?durationHours=48`);
      setMessage(res.data.message || 'Đã mở phúc khảo thành công!');
      if (onRefreshClasses) onRefreshClasses();
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể mở thời hạn phúc khảo.');
    }
  };

  const handleFinalizeClass = async () => {
    if (!selectedClassId) return;
    if (!window.confirm('Bạn có chắc muốn CHỐT ĐIỂM KỲ THI (Finalize)? Toàn bộ điểm sẽ bị khóa và đóng quy trình phúc khảo vĩnh viễn.')) return;

    setError('');
    setMessage('');
    try {
      const res = await api.post(`/exam-classes/${selectedClassId}/finalize`);
      setMessage(res.data.message || 'Lớp thi đã được chốt điểm chính thức!');
      if (onRefreshClasses) onRefreshClasses();
    } catch (err) {
      setError(err.response?.data?.message || 'Không thể chốt điểm kỳ thi.');
    }
  };

  // Assign Reviewer
  const handleAssignReviewer = async (e) => {
    e.preventDefault();
    if (!assignModal.appeal || !assignModal.reviewerLecturerId) return;

    try {
      await api.post(`/appeals/${assignModal.appeal.id}/assign-reviewer`, {
        reviewerLecturerId: Number(assignModal.reviewerLecturerId)
      });
      setMessage('Đã phân công giảng viên chấm phúc khảo độc lập thành công!');
      setAssignModal({ show: false, appeal: null, reviewerLecturerId: '' });
      fetchAppeals();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi phân công giảng viên.');
    }
  };

  // Resolve Appeal
  const handleResolveAppeal = async (e) => {
    e.preventDefault();
    if (!resolveModal.appeal) return;

    try {
      await api.post(`/appeals/${resolveModal.appeal.id}/resolve`, {
        decision: resolveModal.decision,
        finalScore: resolveModal.finalScore ? parseFloat(resolveModal.finalScore) : null,
        resolutionNotes: resolveModal.resolutionNotes || 'Đã xử lý và cập nhật kết quả phúc khảo.'
      });
      setMessage('Đã giải quyết và chốt điểm phúc khảo thành công!');
      setResolveModal({ show: false, appeal: null, decision: 'Approved', finalScore: '', resolutionNotes: '' });
      fetchAppeals();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi giải quyết đơn phúc khảo.');
    }
  };

  // Open Sample Appeal Modal
  const handleOpenCreateSample = async () => {
    if (!selectedClassId) return;
    try {
      const res = await api.get(`/submissions/class/${selectedClassId}`, { params: { pageSize: 50 } });
      const items = res.data.items || [];
      const graded = items.filter((s) => s.status?.toLowerCase() === 'graded');
      setCreateSampleModal({
        show: true,
        submissions: graded,
        submissionId: graded[0]?.id || '',
        reason: 'Em làm đúng bài nhưng điểm nhận được chưa phản ánh đúng tiêu chí số 2.'
      });
    } catch (err) {
      setError('Không thể lấy danh sách bài thi đã chấm để tạo đơn phúc khảo mẫu.');
    }
  };

  // Submit Sample Appeal
  const handleSubmitSampleAppeal = async (e) => {
    e.preventDefault();
    const sub = createSampleModal.submissions.find((s) => s.id === Number(createSampleModal.submissionId));
    if (!sub) return;

    try {
      await api.post('/appeals', {
        examClassId: Number(selectedClassId),
        submissionId: sub.id,
        studentId: sub.studentId,
        studentName: sub.studentName,
        reason: createSampleModal.reason,
        studentEmail: `${sub.studentId.toLowerCase()}@fpt.edu.vn`
      });
      setMessage('Đã tạo đơn phúc khảo thành công!');
      setCreateSampleModal({ show: false, submissions: [], submissionId: '', reason: '' });
      fetchAppeals();
    } catch (err) {
      setError(err.response?.data?.message || 'Lỗi khi tạo đơn phúc khảo.');
    }
  };

  const getStatusBadge = (status) => {
    const config = {
      pending: { label: 'Chờ phân công', bg: 'rgba(234, 179, 8, 0.1)', color: '#eab308' },
      assigned: { label: 'Đang chấm lại', bg: 'rgba(59, 130, 246, 0.1)', color: '#3b82f6' },
      reviewcompleted: { label: 'Đã chấm xong', bg: 'rgba(168, 85, 247, 0.1)', color: '#a855f7' },
      approved: { label: 'Chấp nhận (Đổi điểm)', bg: 'rgba(34, 197, 94, 0.1)', color: '#16a34a' },
      rejected: { label: 'Bác bỏ (Giữ điểm)', bg: 'rgba(239, 68, 68, 0.1)', color: '#dc2626' }
    }[status?.toLowerCase()] || { label: status, bg: 'rgba(255,255,255,0.05)', color: 'var(--text-muted)' };

    return (
      <span style={{ fontSize: '0.75rem', fontWeight: 700, padding: '0.2rem 0.55rem', borderRadius: '4px', background: config.bg, color: config.color }}>
        {config.label}
      </span>
    );
  };

  return (
    <div className="fade-in">
      {/* Top Controls Card */}
      <div className="glass-panel" style={{ marginBottom: '1.5rem', padding: '1.25rem 1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <ClipboardCheck size={22} style={{ color: 'var(--color-primary)' }} />
              Quản Lý Phúc Khảo & Chấm Khảo Sát (Dual-Grading)
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: '0.25rem 0 0' }}>
              Quy trình phúc khảo 2 vòng độc lập: phân công chấm mù (Blind Grading) và chốt điểm chính thức.
            </p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
            <select
              className="form-control"
              style={{ width: '260px', padding: '0.45rem 0.75rem', fontSize: '0.9rem' }}
              value={selectedClassId}
              onChange={(e) => setSelectedClassId(e.target.value)}
            >
              <option value="">-- Chọn Lớp Thi Để Quản Lý --</option>
              {classes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.classCode} - {c.subjectCode} ({c.status})
                </option>
              ))}
            </select>

            {selectedClass && (
              <>
                {selectedClass.status === 'Completed' && (
                  <button
                    onClick={handleOpenAppealDuration}
                    className="btn btn-primary btn-sm"
                    style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}
                  >
                    <Unlock size={14} /> Mở Phúc Khảo (48h)
                  </button>
                )}

                {selectedClass.status === 'AppealOpen' && (
                  <button
                    onClick={handleFinalizeClass}
                    className="btn btn-secondary btn-sm"
                    style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', color: '#ef4444' }}
                    title="Chốt điểm và đóng toàn bộ quy trình phúc khảo"
                  >
                    <Lock size={14} /> Chốt Điểm Lớp (Finalize)
                  </button>
                )}

                <button
                  onClick={handleOpenCreateSample}
                  className="btn btn-secondary btn-sm"
                  style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}
                  title="Tạo đơn phúc khảo mẫu để kiểm thử quy trình"
                >
                  <Plus size={14} /> + Đơn Mẫu
                </button>
              </>
            )}
          </div>
        </div>

        {/* Status Messages */}
        {error && (
          <div style={{ marginTop: '1rem', padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '6px', color: '#ef4444', fontSize: '0.85rem' }}>
            {error}
          </div>
        )}
        {message && (
          <div style={{ marginTop: '1rem', padding: '0.75rem 1rem', background: 'rgba(34, 197, 94, 0.1)', border: '1px solid rgba(34, 197, 94, 0.3)', borderRadius: '6px', color: '#16a34a', fontSize: '0.85rem' }}>
            {message}
          </div>
        )}
      </div>

      {/* Appeals List Table */}
      <div className="glass-panel" style={{ overflow: 'hidden' }}>
        <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>Danh Sách Đơn Phúc Khảo ({appeals.length})</span>
          {selectedClassId && (
            <button onClick={fetchAppeals} className="btn btn-secondary btn-sm" style={{ padding: '0.25rem 0.5rem' }}>
              <RefreshCw size={13} className={loading ? 'spin' : ''} /> Làm mới
            </button>
          )}
        </div>

        {!selectedClassId ? (
          <div style={{ textAlign: 'center', padding: '3.5rem 1rem', color: 'var(--text-muted)' }}>
            <ClipboardCheck size={44} style={{ strokeWidth: 1.2, margin: '0 auto 0.75rem', display: 'block' }} />
            <p>Vui lòng chọn một Lớp thi ở trên để xem và xử lý các đơn phúc khảo.</p>
          </div>
        ) : loading && appeals.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
            Đang tải danh sách đơn phúc khảo...
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table className="custom-table" style={{ margin: 0 }}>
              <thead>
                <tr>
                  <th>Mã đơn</th>
                  <th>Sinh viên</th>
                  <th>Điểm Gốc</th>
                  <th>Lý do khiếu nại</th>
                  <th>Trạng thái</th>
                  <th>Giảng viên chấm lại</th>
                  <th>Điểm chấm lại</th>
                  <th>Điểm chốt</th>
                  <th style={{ textAlign: 'center' }}>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {appeals.length === 0 ? (
                  <tr>
                    <td colSpan="9" style={{ textAlign: 'center', padding: '2.5rem', color: 'var(--text-muted)' }}>
                      Chưa có đơn phúc khảo nào cho lớp thi này.
                    </td>
                  </tr>
                ) : (
                  appeals.map((a) => (
                    <tr key={a.id}>
                      <td style={{ fontWeight: 600 }}>#{a.id}</td>
                      <td>
                        <b style={{ color: 'var(--text-main)' }}>{a.studentName}</b>
                        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', display: 'block' }}>{a.studentId}</span>
                      </td>
                      <td style={{ fontWeight: 700 }}>
                        {a.originalScore.toFixed(2)}đ ({a.originalLetterGrade})
                      </td>
                      <td style={{ maxWidth: '200px', fontSize: '0.8rem', color: 'var(--text-muted)' }} title={a.reason}>
                        <div style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {a.reason}
                        </div>
                      </td>
                      <td>{getStatusBadge(a.status)}</td>
                      <td>
                        {a.reviewerLecturerName ? (
                          <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--color-primary)' }}>
                            {a.reviewerLecturerName}
                          </span>
                        ) : (
                          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>-</span>
                        )}
                      </td>
                      <td style={{ fontWeight: 700, color: a.reviewScore !== null ? '#3b82f6' : 'var(--text-muted)' }}>
                        {a.reviewScore !== null ? `${a.reviewScore.toFixed(2)}đ` : '-'}
                      </td>
                      <td style={{ fontWeight: 800, color: a.finalScore !== null ? '#16a34a' : 'var(--text-muted)' }}>
                        {a.finalScore !== null ? `${a.finalScore.toFixed(2)}đ (${a.finalLetterGrade})` : '-'}
                      </td>
                      <td style={{ textAlign: 'center' }}>
                        {a.status === 'Pending' && (
                          <button
                            onClick={() => setAssignModal({ show: true, appeal: a, reviewerLecturerId: '' })}
                            className="btn btn-primary btn-sm"
                            style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem' }}
                          >
                            <UserCheck size={13} /> Phân Công
                          </button>
                        )}

                        {a.status === 'ReviewCompleted' && (
                          <button
                            onClick={() =>
                              setResolveModal({
                                show: true,
                                appeal: a,
                                decision: 'Approved',
                                finalScore: a.reviewScore !== null ? a.reviewScore.toString() : '',
                                resolutionNotes: ''
                              })
                            }
                            className="btn btn-primary btn-sm"
                            style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem', background: '#16a34a', borderColor: '#16a34a' }}
                          >
                            <CheckCircle size={13} /> Quyết Định
                          </button>
                        )}

                        {(a.status === 'Approved' || a.status === 'Rejected') && (
                          <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', fontStyle: 'italic' }}>
                            Đã hoàn tất
                          </span>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Assign Reviewer Modal */}
      {assignModal.show && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(5px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1250 }}>
          <div className="glass-panel fade-in" style={{ width: '100%', maxWidth: '480px', padding: '1.5rem', border: '1px solid var(--panel-border)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h4 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700, color: 'var(--color-primary)' }}>
                Phân Công Giảng Viên Chấm Phúc Khảo
              </h4>
              <button onClick={() => setAssignModal({ show: false, appeal: null, reviewerLecturerId: '' })} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleAssignReviewer}>
              <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: '1rem' }}>
                Sinh viên: <b>{assignModal.appeal?.studentName}</b> ({assignModal.appeal?.studentId})
                <br />
                Điểm ban đầu: <b>{assignModal.appeal?.originalScore.toFixed(2)}đ</b>
              </p>

              <div className="form-group" style={{ marginBottom: '1.25rem' }}>
                <label className="form-label">Chọn Giảng viên chấm khảo sát vòng 2 (Blind Grader):</label>
                <select
                  className="form-control"
                  value={assignModal.reviewerLecturerId}
                  onChange={(e) => setAssignModal({ ...assignModal, reviewerLecturerId: e.target.value })}
                  required
                >
                  <option value="">-- Chọn Giảng viên --</option>
                  {lecturers.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.fullName || l.userName} ({l.email})
                    </option>
                  ))}
                </select>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.35rem', display: 'block' }}>
                  * Lưu ý: Giảng viên được phân công sẽ không nhìn thấy tên sinh viên và điểm số của người chấm vòng 1.
                </span>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                <button type="button" onClick={() => setAssignModal({ show: false, appeal: null, reviewerLecturerId: '' })} className="btn btn-secondary btn-sm">
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary btn-sm">
                  Xác Nhận Phân Công
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Resolve Appeal Modal */}
      {resolveModal.show && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(5px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1250 }}>
          <div className="glass-panel fade-in" style={{ width: '100%', maxWidth: '520px', padding: '1.5rem', border: '1px solid var(--panel-border)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h4 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700, color: 'var(--color-primary)' }}>
                Quyết Định Xử Lý Đơn Phúc Khảo
              </h4>
              <button onClick={() => setResolveModal({ show: false, appeal: null, decision: 'Approved', finalScore: '', resolutionNotes: '' })} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleResolveAppeal}>
              {/* Comparison Box */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', background: 'rgba(255,255,255,0.02)', padding: '0.85rem', borderRadius: '8px', border: '1px solid var(--panel-border)', marginBottom: '1rem' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Điểm Ban Đầu:</span>
                  <span style={{ fontSize: '1.2rem', fontWeight: 700, color: '#ef4444' }}>
                    {resolveModal.appeal?.originalScore.toFixed(2)}đ
                  </span>
                </div>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Điểm Chấm Lại (Vòng 2):</span>
                  <span style={{ fontSize: '1.2rem', fontWeight: 700, color: '#3b82f6' }}>
                    {resolveModal.appeal?.reviewScore !== null ? `${resolveModal.appeal?.reviewScore.toFixed(2)}đ` : '-'}
                  </span>
                </div>
              </div>

              {resolveModal.appeal?.reviewComments && (
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', background: 'rgba(0,0,0,0.2)', padding: '0.5rem 0.75rem', borderRadius: '6px', marginBottom: '1rem' }}>
                  <b>Nhận xét người chấm lại:</b> "{resolveModal.appeal.reviewComments}"
                </div>
              )}

              {/* Decision Radio */}
              <div className="form-group" style={{ marginBottom: '1rem' }}>
                <label className="form-label">Quyết định của Khảo thí:</label>
                <div style={{ display: 'flex', gap: '1.5rem', marginTop: '0.35rem' }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer', fontSize: '0.9rem' }}>
                    <input
                      type="radio"
                      name="decision"
                      value="Approved"
                      checked={resolveModal.decision === 'Approved'}
                      onChange={() =>
                        setResolveModal({
                          ...resolveModal,
                          decision: 'Approved',
                          finalScore: resolveModal.appeal?.reviewScore !== null ? resolveModal.appeal.reviewScore.toString() : ''
                        })
                      }
                    />
                    <span style={{ color: '#16a34a', fontWeight: 600 }}>Chấp Nhận (Cập nhật điểm mới)</span>
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer', fontSize: '0.9rem' }}>
                    <input
                      type="radio"
                      name="decision"
                      value="Rejected"
                      checked={resolveModal.decision === 'Rejected'}
                      onChange={() =>
                        setResolveModal({
                          ...resolveModal,
                          decision: 'Rejected',
                          finalScore: resolveModal.appeal?.originalScore.toString() || ''
                        })
                      }
                    />
                    <span style={{ color: '#ef4444', fontWeight: 600 }}>Bác Bỏ (Giữ điểm gốc)</span>
                  </label>
                </div>
              </div>

              {/* Final Score Input */}
              <div className="form-group" style={{ marginBottom: '1rem' }}>
                <label className="form-label">Điểm Chốt Cuối Cùng (Thang 10):</label>
                <input
                  type="number"
                  step="0.05"
                  min="0"
                  max="10"
                  required
                  value={resolveModal.finalScore}
                  onChange={(e) => setResolveModal({ ...resolveModal, finalScore: e.target.value })}
                  className="form-control"
                  style={{ width: '120px', fontWeight: 700 }}
                />
              </div>

              {/* Notes */}
              <div className="form-group" style={{ marginBottom: '1.25rem' }}>
                <label className="form-label">Ghi Chú Kết Luận:</label>
                <textarea
                  className="form-control"
                  rows={2}
                  value={resolveModal.resolutionNotes}
                  onChange={(e) => setResolveModal({ ...resolveModal, resolutionNotes: e.target.value })}
                  placeholder="Ghi rõ lý do phê duyệt / từ chối khiếu nại..."
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                <button type="button" onClick={() => setResolveModal({ show: false, appeal: null, decision: 'Approved', finalScore: '', resolutionNotes: '' })} className="btn btn-secondary btn-sm">
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary btn-sm" style={{ background: '#16a34a', borderColor: '#16a34a' }}>
                  Xác Nhận & Cập Nhật Điểm
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Sample Appeal Creation Modal */}
      {createSampleModal.show && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.65)', backdropFilter: 'blur(5px)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1250 }}>
          <div className="glass-panel fade-in" style={{ width: '100%', maxWidth: '480px', padding: '1.5rem', border: '1px solid var(--panel-border)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
              <h4 style={{ margin: 0, fontSize: '1.1rem', fontWeight: 700, color: 'var(--color-primary)' }}>
                Tạo Đơn Phúc Khảo Mẫu (Testing)
              </h4>
              <button onClick={() => setCreateSampleModal({ show: false, submissions: [], submissionId: '', reason: '' })} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleSubmitSampleAppeal}>
              <div className="form-group" style={{ marginBottom: '1rem' }}>
                <label className="form-label">Chọn Bài Nộp Của Sinh Viên:</label>
                <select
                  className="form-control"
                  value={createSampleModal.submissionId}
                  onChange={(e) => setCreateSampleModal({ ...createSampleModal, submissionId: e.target.value })}
                  required
                >
                  {createSampleModal.submissions.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.studentName} ({s.studentId}) - Điểm: {s.totalScore?.toFixed(2) || '0'}đ
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group" style={{ marginBottom: '1.25rem' }}>
                <label className="form-label">Lý Do Khiếu Nại:</label>
                <textarea
                  className="form-control"
                  rows={3}
                  value={createSampleModal.reason}
                  onChange={(e) => setCreateSampleModal({ ...createSampleModal, reason: e.target.value })}
                  required
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                <button type="button" onClick={() => setCreateSampleModal({ show: false, submissions: [], submissionId: '', reason: '' })} className="btn btn-secondary btn-sm">
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary btn-sm">
                  Tạo Đơn Phúc Khảo
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
