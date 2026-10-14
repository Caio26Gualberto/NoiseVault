export const myTicketsPath = '/suporte'
export const adminSupportPath = '/admin/suporte'

export const myTicketPath = (id: number) => `${myTicketsPath}/${id}`
export const adminTicketPath = (id: number) => `${adminSupportPath}/${id}`
