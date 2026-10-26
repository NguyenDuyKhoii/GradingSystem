import React, { useState, useEffect } from 'react';
import { Award, X, AlertCircle, CheckCircle, FileText, Send } from 'lucide-react';
import api from '../api';

export default function BlindReviewModal({ isOpen, onClose, appealId, onReviewSubmitted }) {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [criteriaScores, setCriteriaScores] = useState({});
  const [reviewerFeedback, setReviewerFeedback] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen && appealId) {
      setError('');
      setCriteriaScores({});
      setReviewerFeedback('');
      fetchBlindReviewData();
    }
  }, [isOpen, appealId]);

  const fetchBlindReviewData = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await api.get(`/appeals/${appealId}/blind-review`);
      setData(res.data);
      // Initialize criteria scores
      if (res.data.rubric?.criteria) {
        const initial = {};
        res.data.rubric.criteria.forEach((c) => {
          initial[c.id] = c.maxPoints || 0;
        });
        setCriteriaScores(initial);
      }
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'Không thể tải thông tin bài chấm phúc khảo.');
    } finally {
      setLoading(false);
    }
  };

  const handleScoreChange = (criteriaId, value, maxPoints) => {
    let num = parseFloat(value);
    if (isNaN(num)) num = 0;
    if (num < 0) num = 0;
    if (num > maxPoints) num = maxPoints;
    setCriteriaScores((prev) => ({
      ...prev,
      [criteriaId]: num
    }));
  };

  const calculateTotal = () => {
    if (!data?.rubric?.criteria) return 0;
    let total = 0;
    data.rubric.criteria.forEach((c) => {
      const score = criteriaScores[c.id] || 0;
      const weight = (c.weight || 0) / 100;
      total += score * weight;
    });
    return total.toFixed(2);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!data?.rubric?.criteria) return;

    setSubmitting(true);
    setError('');

    const details = data.rubric.criteria.map((c) => ({
      rubricCriteriaId: c.id,
      score: criteriaScores[c.id] || 0,
      comments: ''
    }));

    try {
      await api.post(`/appeals/${appealId}/submit-review`, {
        reviewerFeedback: reviewerFeedback || 'Đã hoàn thành chấm khảo sát độc lập (Blind Review).',
        details
      });
      alert('Đã gửi điểm chấm phúc khảo thành công!');
      if (onReviewSubmitted) onReviewSubmitted();
      onClose();
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'Có lỗi xảy ra khi nộp điểm phúc khảo.');
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen || !appealId) return null;

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0, 0, 0, 0.75)',
        backdropFilter: 'blur(6px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1300,
        padding: '1rem'
      }}
    >
      <div
        className="glass-panel fade-in"
        style={{
          width: '100%',
          maxWidth: '820px',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          border: '1px solid var(--panel-border)',
          overflow: 'hidden'
        }}
      >
        {/* Header */}
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
                background: 'rgba(242, 113, 33, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--color-primary)'
              }}
            >
              <Award size={20} />
            </div>
            <div>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
                Chấm Phúc Khảo Độc Lập (Blind Dual-Grading)
              </h3>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                Bảo mật thông tin sinh viên & người chấm vòng 1 để đảm bảo tính khách quan tuyệt đối.
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
              padding: '0.4rem'
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Content */}
        <div style={{ padding: '1.25rem 1.5rem', overflowY: 'auto', flex: 1 }}>
          {loading ? (
            <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
              Đang tải barem và bài nộp cần chấm lại...
            </div>
          ) : error ? (
            <div style={{ padding: '1rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.3)', borderRadius: '8px', color: '#ef4444' }}>
              {error}
            </div>
          ) : data ? (
            <form onSubmit={handleSubmit}>
              {/* Anonymous Banner */}
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '0.85rem 1rem',
                  background: 'rgba(59, 130, 246, 0.08)',
                  border: '1px solid rgba(59, 130, 246, 0.25)',
                  borderRadius: '8px',
                  marginBottom: '1.25rem'
                }}
              >
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Mã bài thi ẩn danh:</span>
                  <b style={{ color: '#60a5fa', fontSize: '1rem' }}>{data.anonymousStudentCode}</b>
                  <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginLeft: '0.75rem' }}>
                    Môn: <b>{data.subjectCode} - {data.subjectName}</b>
                  </span>
                </div>

                <div style={{ textAlign: 'right' }}>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block' }}>Tổng điểm chấm lại:</span>
                  <span style={{ fontSize: '1.3rem', fontWeight: 800, color: 'var(--color-primary)' }}>
                    {calculateTotal()} / 10
                  </span>
                </div>
              </div>

              {/* Student's Reason */}
              <div
                style={{
                  background: 'rgba(255, 255, 255, 0.02)',
                  border: '1px solid var(--panel-border)',
                  borderRadius: '8px',
                  padding: '0.85rem 1rem',
                  marginBottom: '1.25rem'
                }}
              >
                <div style={{ fontSize: '0.75rem', fontWeight: 700, color: 'var(--color-warning)', marginBottom: '0.3rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                  <AlertCircle size={14} /> Lý do khiếu nại của sinh viên:
                </div>
                <div style={{ fontSize: '0.85rem', color: 'var(--text-main)', fontStyle: 'italic' }}>
                  "{data.studentReason || 'Không có lý do cụ thể.'}"
                </div>
              </div>

              {/* Rubric Criteria Form */}
              <h4 style={{ fontSize: '0.95rem', fontWeight: 700, marginBottom: '0.75rem', color: 'var(--text-main)' }}>
                Barem Điểm & Tiêu Chí Đánh Giá ({data.rubric?.name})
              </h4>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginBottom: '1.25rem' }}>
                {data.rubric?.criteria?.map((c) => (
                  <div
                    key={c.id}
                    style={{
                      background: 'rgba(255, 255, 255, 0.03)',
                      border: '1px solid var(--panel-border)',
                      borderRadius: '8px',
                      padding: '0.85rem 1rem',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      gap: '1rem',
                      flexWrap: 'wrap'
                    }}
                  >
                    <div style={{ flex: 1, minWidth: '220px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.2rem' }}>
                        <span style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--text-main)' }}>
                          {c.criteriaName}
                        </span>
                        <span style={{ fontSize: '0.75rem', padding: '0.1rem 0.4rem', borderRadius: '4px', background: 'rgba(242, 113, 33, 0.1)', color: 'var(--color-primary)' }}>
                          Trọng số: {c.weight}%
                        </span>
                      </div>
                      <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{c.description}</span>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <input
                        type="number"
                        step="0.25"
                        min="0"
                        max={c.maxPoints}
                        value={criteriaScores[c.id] ?? ''}
                        onChange={(e) => handleScoreChange(c.id, e.target.value, c.maxPoints)}
                        style={{
                          width: '75px',
                          padding: '0.4rem 0.5rem',
                          borderRadius: '6px',
                          border: '1px solid var(--panel-border)',
                          background: 'var(--panel-bg)',
                          color: 'var(--text-main)',
                          fontWeight: 700,
                          textAlign: 'center'
                        }}
                        required
                      />
                      <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>/ {c.maxPoints}đ</span>
                    </div>
                  </div>
                ))}
              </div>

              {/* Reviewer Feedback */}
              <div className="form-group" style={{ marginBottom: '1rem' }}>
                <label className="form-label">Nhận xét của Cán bộ Chấm Phúc Khảo (Feedback):</label>
                <textarea
                  className="form-control"
                  rows={3}
                  value={reviewerFeedback}
                  onChange={(e) => setReviewerFeedback(e.target.value)}
                  placeholder="Ghi rõ nhận xét bài làm, lý do tăng/giảm điểm so với barem..."
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                <button type="button" onClick={onClose} className="btn btn-secondary btn-sm">
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="btn btn-primary btn-sm"
                  style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
                >
                  <Send size={14} />
                  {submitting ? 'Đang nộp...' : 'Xác Nhận & Nộp Điểm Chấm Lại'}
                </button>
              </div>
            </form>
          ) : null}
        </div>
      </div>
    </div>
  );
}
