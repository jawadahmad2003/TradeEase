import { useEffect, useMemo, useState } from "react";
import { api } from "../api/client";
import { useToast } from "../components/Toast";

export default function UserManagement({ setActivePage }) {
  const showToast = useToast();
  const [search, setSearch] = useState("");
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  const loadUsers = () => {
    setLoading(true);
    api.adminUsers()
      .then((data) => setUsers(data.map((u) => ({
        id: u.userId,
        name: u.fullName,
        email: u.email,
        role: u.role,
        company: u.businessName || "—",
        status: u.isActive ? "Active" : "Inactive",
        lastLogin: u.lastLogin
          ? new Date(u.lastLogin).toLocaleString("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" })
          : "Not signed in",
        verified: u.isVerified,
      }))))
      .catch((err) => showToast(err.message, "error"))
      .finally(() => setLoading(false));
  };

  useEffect(() => { loadUsers(); }, []);

  const filteredUsers = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return users;
    return users.filter((user) => user.name.toLowerCase().includes(term) || user.email.toLowerCase().includes(term));
  }, [search, users]);

  const toggleStatus = async (user) => {
    try {
      await api.updateUserStatus(user.id, user.status !== "Active");
      showToast("User status updated.");
      loadUsers();
    } catch (err) {
      showToast(err.message, "error");
    }
  };

  if (loading) return <div className="card"><p className="muted-text">Loading users...</p></div>;

  return (
    <div className="user-management-page admin-page">
      {setActivePage && (
        <button type="button" className="link-btn back-link" onClick={() => setActivePage("admin-portal")}>← Back to Admin Portal</button>
      )}
      <section className="card user-management-card">
        <div className="user-management-head">
          <div>
            <h2>User Management (Admin)</h2>
            <p>Activate and deactivate trader accounts</p>
          </div>
          <div className="user-mgmt-actions">
            <div className="search-wrap user-search">
              <input className="search-input" placeholder="Search by name or email..." value={search} onChange={(e) => setSearch(e.target.value)} />
            </div>
          </div>
        </div>

        <div className="table-wrap">
          <table className="data-table user-table">
            <thead>
              <tr>
                <th>USER</th>
                <th>ROLE</th>
                <th>COMPANY</th>
                <th>STATUS</th>
                <th>VERIFIED</th>
                <th>LAST LOGIN</th>
                <th>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.email} className="table-row-hover">
                  <td>
                    <div className="user-cell">
                      <div className="user-avatar-table">{initials(user.name)}</div>
                      <div>
                        <div className="user-table-name">{user.name}</div>
                        <div className="user-table-email">{user.email}</div>
                      </div>
                    </div>
                  </td>
                  <td>{user.role}</td>
                  <td>{user.company}</td>
                  <td><span className={`user-status ${user.status.toLowerCase()}`}>{user.status}</span></td>
                  <td>{user.verified ? <span className="verified-badge">Verified</span> : "—"}</td>
                  <td>{user.lastLogin}</td>
                  <td>
                    <button type="button" className="btn-outline-sm" onClick={() => toggleStatus(user)}>
                      {user.status === "Active" ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

function initials(name) {
  return name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
}
