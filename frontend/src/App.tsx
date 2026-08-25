import { BrowserRouter, Route, Routes } from 'react-router-dom';
import NotesList from './pages/NotesList';
import ViewNote from './pages/ViewNote';
import CreateNote from './pages/CreateNote';
import EditNote from './pages/EditNote';

function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <Routes>
          <Route path="/" element={<NotesList />} />
          <Route path="/notes/new" element={<CreateNote />} />
          <Route path="/notes/:id" element={<ViewNote />} />
          <Route path="/notes/:id/edit" element={<EditNote />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}

export default App;
